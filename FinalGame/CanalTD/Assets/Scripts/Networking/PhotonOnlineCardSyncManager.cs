/*
 * File: PhotonOnlineCardSyncManager.cs
 *
 * Purpose:
 * Implements PhotonOnlineCardSyncManager for the networking layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Online scene managers or networking helper objects used during lobby and match flow.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for PhotonOnlineCardSyncManager within the networking system.
 * - Coordinate related objects, state changes, and cross-system communication.
 * - Keep multiplayer state aligned while respecting online lifecycle guards and scene context.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Photon room/player state, network events, and authoritative sync payloads when online mode is active.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Sends, applies, or guards online sync operations without changing project-level Photon settings.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify PhotonOnlineCardSyncManager in the scene or prefab where it is used and confirm the main happy path still works.
 * - Retest both single-client and multi-client online flows after editing this script.
 */
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if PHOTON_UNITY_NETWORKING
using ExitGames.Client.Photon;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun;
using Photon.Realtime;
#endif

#if PHOTON_UNITY_NETWORKING
public class PhotonOnlineCardSyncManager : MonoBehaviourPunCallbacks, IOnEventCallback
#else
public class PhotonOnlineCardSyncManager : MonoBehaviour
#endif
{
#if PHOTON_UNITY_NETWORKING
    private const byte CardSyncRequestEventCode = 21;
    private const byte CardSyncApplyEventCode = 22;
    private const byte CardSyncPrivateHandEventCode = 23;
    private const byte CardSyncPrivateHandRequestEventCode = 24;
#endif

    public static PhotonOnlineCardSyncManager Instance { get; private set; }

    [Header("Managers")]
    public PlayerManager playerManager;
    public CardDrawManager cardDrawManager;
    public TurnManager turnManager;
    public PhotonOnlineGameSceneManager onlineGameSceneManager;

    private readonly List<string> deckCardIds = new List<string>();
    private readonly Dictionary<int, List<string>> playerHandCardIds = new Dictionary<int, List<string>>();
    private readonly Dictionary<string, TowerBuildArea> tileTargetsById = new Dictionary<string, TowerBuildArea>();
    private bool initializedForOnlineMatch;
    private bool photonCallbacksRegistered;

    public bool IsInitializedForOnlineMatch
    {
        get { return initializedForOnlineMatch; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        AutoAssignReferences();
    }

    private void OnDisable()
    {
        UnregisterPhotonCallbacksIfNeeded();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void InitializeForOnlineMatch()
    {
        AutoAssignReferences();

#if PHOTON_UNITY_NETWORKING
        if (!CanRunOnlineGameSceneCardSync() ||
            PhotonNetwork.CurrentRoom == null ||
            cardDrawManager == null)
        {
            return;
        }

        RegisterPhotonCallbacksIfNeeded();

        bool wasInitialized = initializedForOnlineMatch;
        initializedForOnlineMatch = true;

        if (!wasInitialized)
        {
            cardDrawManager.ClearAllPlayerHandsForOnlineBootstrap();
        }

        if (PhotonNetwork.IsMasterClient)
        {
            BootstrapAuthoritativeStateAsMaster();
        }
        else
        {
            ApplySnapshotFromRoomProperties();
            RequestPrivateHandStateFromMaster();
        }
#endif
    }

    public void HandleRoomPropertiesUpdated()
    {
#if PHOTON_UNITY_NETWORKING
        if (!initializedForOnlineMatch || !CanRunOnlineGameSceneCardSync())
        {
            return;
        }

        ApplySnapshotFromRoomProperties();
#endif
    }

    public bool RequestDrawCard()
    {
        return RequestAction(OnlineCardActionType.Draw, string.Empty, string.Empty, string.Empty, -1);
    }

    public bool RequestDiscardCard(string cardId, string cardName)
    {
        return RequestAction(OnlineCardActionType.Discard, cardId, cardName, string.Empty, -1);
    }

    public bool RequestPlayCard(string cardId, string cardName, string targetId, int targetPlayerId)
    {
        string normalizedCardId = CardDrawManager.NormalizeCardId(cardId);

        if (IsPlayerInteractionCardId(normalizedCardId))
        {
            Debug.Log(
                "RequestCardEffect" +
                " actorPlayerId=" + (onlineGameSceneManager != null ? onlineGameSceneManager.LocalPlayerId : -1) +
                " targetPlayerId=" + targetPlayerId +
                " cardId=" + normalizedCardId +
                " cardName=" + cardName
            );
        }
        else if (IsTakeOverCardId(normalizedCardId))
        {
            Debug.Log(
                "RequestTakeOver" +
                " actorPlayerId=" + (onlineGameSceneManager != null ? onlineGameSceneManager.LocalPlayerId : -1) +
                " targetTileId=" + targetId +
                " cardId=" + normalizedCardId +
                " cardName=" + cardName
            );
        }
        else if (IsFreezeClaimCardId(normalizedCardId))
        {
            Debug.Log(
                "RequestFreezeClaim" +
                " actorPlayerId=" + (onlineGameSceneManager != null ? onlineGameSceneManager.LocalPlayerId : -1) +
                " targetTileId=" + targetId +
                " cardId=" + normalizedCardId +
                " cardName=" + cardName
            );
        }

        return RequestAction(OnlineCardActionType.Play, cardId, cardName, targetId, targetPlayerId);
    }

    public void OnEvent(EventData photonEvent)
    {
#if PHOTON_UNITY_NETWORKING
        if (!CanRunOnlineGameSceneCardSync())
        {
            return;
        }

        if (photonEvent.Code == CardSyncRequestEventCode)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            OnlineCardRequestData request = DeserializeRequest(photonEvent.CustomData as string);

            if (request != null)
            {
                ProcessRequestAsMaster(request, photonEvent.Sender);
            }

            return;
        }

        if (photonEvent.Code == CardSyncPrivateHandRequestEventCode)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                ProcessPrivateHandStateRequest(photonEvent);
            }

            return;
        }

        if (photonEvent.Code == CardSyncApplyEventCode)
        {
            OnlineCardApplyData applyData = DeserializeApply(photonEvent.CustomData as string);

            if (applyData != null)
            {
                ApplyConfirmedAction(applyData);
            }

            return;
        }

        if (photonEvent.Code != CardSyncPrivateHandEventCode)
        {
            return;
        }

        OnlinePrivateHandStateData privateState = DeserializePrivateState(photonEvent.CustomData as string);

        if (privateState != null)
        {
            ApplyPrivateHandState(privateState);
        }
#endif
    }

    private bool RequestAction(
        OnlineCardActionType actionType,
        string cardId,
        string cardName,
        string targetId,
        int targetPlayerId)
    {
#if !PHOTON_UNITY_NETWORKING
        return false;
#else
        if (!CanRunOnlineGameSceneCardSync())
        {
            return false;
        }

        AutoAssignReferences();

        if (onlineGameSceneManager == null || cardDrawManager == null || !initializedForOnlineMatch)
        {
            return false;
        }

        if (!OnlineTurnPermissionManager.CanLocalPlayerAct(true))
        {
            return false;
        }

        OnlineCardRequestData request = new OnlineCardRequestData
        {
            actionType = actionType,
            actorPlayerId = onlineGameSceneManager.LocalPlayerId,
            currentTurnPlayerId = onlineGameSceneManager.CurrentTurnPlayerId,
            cardId = string.IsNullOrWhiteSpace(cardId) ? string.Empty : cardId,
            cardName = string.IsNullOrWhiteSpace(cardName) ? string.Empty : cardName,
            targetId = string.IsNullOrWhiteSpace(targetId) ? string.Empty : targetId,
            targetPlayerId = targetPlayerId,
            timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        if (PhotonNetwork.IsMasterClient)
        {
            ProcessRequestAsMaster(request, PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1);
            return true;
        }

        PhotonNetwork.RaiseEvent(
            CardSyncRequestEventCode,
            JsonUtility.ToJson(request),
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );
        return true;
#endif
    }

#if PHOTON_UNITY_NETWORKING
    private void ProcessRequestAsMaster(OnlineCardRequestData request, int senderActorNumber)
    {
        if (!CanRunOnlineGameSceneCardSync())
        {
            return;
        }

        AutoAssignReferences();

        OnlineCardApplyData applyData = ValidateRequest(request, senderActorNumber);

        if (!applyData.accepted)
        {
            SendApplyToRequester(senderActorNumber, applyData);
            return;
        }

        OnlineCardPublicSnapshot beforeSnapshot = BuildPublicSnapshot();
        ApplyAuthoritativeMutation(request, applyData);
        OnlineCardPublicSnapshot afterSnapshot = BuildPublicSnapshot();
        LogAuthoritativeHandCountTransition(request, applyData, beforeSnapshot, afterSnapshot);
        UpdateSnapshotProperty();
        BroadcastApplyToAll(applyData);
        SendPrivateHandStatesForAcceptedAction(applyData);
    }

    private OnlineCardApplyData ValidateRequest(OnlineCardRequestData request, int senderActorNumber)
    {
        OnlineCardApplyData result = new OnlineCardApplyData
        {
            actionType = request.actionType,
            actorPlayerId = request.actorPlayerId,
            cardId = request.actionType == OnlineCardActionType.Play ? request.cardId : string.Empty,
            cardName = request.actionType == OnlineCardActionType.Play ? request.cardName : string.Empty,
            targetId = request.targetId,
            targetTileId = request.targetId,
            targetPlayerId = request.targetPlayerId,
            timestamp = request.timestamp,
            accepted = false
        };

        int resolvedSenderPlayerId = ResolvePlayerIdForActorNumber(senderActorNumber);

        if (!PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext())
        {
            return RejectValidation(request, result, "Online game inactive.");
        }

        if (!initializedForOnlineMatch)
        {
            return RejectValidation(request, result, "Card sync not ready.");
        }

        if (resolvedSenderPlayerId != request.actorPlayerId)
        {
            return RejectValidation(request, result, "Sender mismatch.");
        }

        if (onlineGameSceneManager == null ||
            onlineGameSceneManager.CurrentTurnPlayerId != request.actorPlayerId ||
            request.currentTurnPlayerId != request.actorPlayerId)
        {
            return RejectValidation(request, result, "Not current turn player.");
        }

        List<string> authoritativeHand = GetOrCreateAuthoritativeHand(request.actorPlayerId);
        int handLimit = cardDrawManager != null ? cardDrawManager.GetMaxHandSizeForPlayer(request.actorPlayerId) : 5;

        switch (request.actionType)
        {
            case OnlineCardActionType.Draw:
                if (turnManager != null && !turnManager.CanDrawCard())
                {
                    return RejectValidation(request, result, "Draw already used.");
                }

                if (deckCardIds.Count <= 0)
                {
                    return RejectValidation(request, result, "Deck is empty.");
                }

                if (authoritativeHand.Count >= handLimit)
                {
                    return RejectValidation(request, result, "Hand limit reached.");
                }
                break;

            case OnlineCardActionType.Discard:
                if (turnManager != null && !turnManager.CanDiscardCard())
                {
                    return RejectValidation(request, result, "Discard already used.");
                }

                if (!HandContainsCardId(authoritativeHand, request.cardId))
                {
                    return RejectValidation(request, result, "Card not in hand.");
                }
                break;

            case OnlineCardActionType.Play:
                if (turnManager != null && !turnManager.CanPlayCard())
                {
                    return RejectValidation(request, result, "Play unavailable.");
                }

                if (!HandContainsCardId(authoritativeHand, request.cardId))
                {
                    return RejectValidation(request, result, "Card not in hand.");
                }

                if (!HasValidTargetMetadata(request.cardId, request.targetId, request.targetPlayerId, request.actorPlayerId))
                {
                    return RejectValidation(request, result, "Invalid target metadata.");
                }

                if (IsPlayerInteractionCardId(request.cardId))
                {
                    string cardEffectRejectReason = ValidatePlayerInteractionCardEffect(request);

                    if (!string.IsNullOrWhiteSpace(cardEffectRejectReason))
                    {
                        return RejectValidation(request, result, cardEffectRejectReason);
                    }
                }
                else if (IsLandControlCardId(request.cardId))
                {
                    string tileRejectReason = ValidateLandControlCardEffect(request, result);

                    if (!string.IsNullOrWhiteSpace(tileRejectReason))
                    {
                        return RejectValidation(request, result, tileRejectReason);
                    }
                }
                break;

            default:
                return RejectValidation(request, result, "Unsupported card action.");
        }

        result.accepted = true;
        LogCardEffectValidation(request, result);

        if (IsTakeOverCardId(request.cardId))
        {
            LogLandControlValidation("ValidateTakeOver", request, result, "(none)");
        }
        else if (IsFreezeClaimCardId(request.cardId))
        {
            LogLandControlValidation("ValidateFreezeClaim", request, result, "(none)");
        }

        return result;
    }

    private void ApplyAuthoritativeMutation(OnlineCardRequestData request, OnlineCardApplyData applyData)
    {
        List<string> authoritativeHand = GetOrCreateAuthoritativeHand(request.actorPlayerId);
        string normalizedCardId = CardDrawManager.NormalizeCardId(request.cardId);

        if (request.actionType == OnlineCardActionType.Draw)
        {
            string drawnCardId = deckCardIds[0];
            deckCardIds.RemoveAt(0);
            authoritativeHand.Add(drawnCardId);
        }
        else if (request.actionType == OnlineCardActionType.Discard)
        {
            string removedCardId;
            RemoveCardIdFromHand(authoritativeHand, request.cardId, out removedCardId);
            deckCardIds.Add(string.IsNullOrWhiteSpace(removedCardId) ? request.cardId : removedCardId);
            ShuffleDeckCardIds();
        }
        else if (request.actionType == OnlineCardActionType.Play)
        {
            if (IsStealCardId(normalizedCardId))
            {
                ApplyStealCardEffect(request, authoritativeHand, applyData);
            }
            else if (IsTradeHandsCardId(normalizedCardId))
            {
                ApplyTradeHandsEffect(request, authoritativeHand, applyData);
            }
            else if (IsDisruptCardId(normalizedCardId))
            {
                ApplyDisruptEffect(request, authoritativeHand, applyData);
            }
            else if (IsTakeOverCardId(normalizedCardId))
            {
                ApplyTakeOverEffect(request, authoritativeHand, applyData);
            }
            else if (IsFreezeClaimCardId(normalizedCardId))
            {
                ApplyFreezeClaimEffect(request, authoritativeHand, applyData);
            }
            else
            {
                RemoveCardIdFromHand(authoritativeHand, request.cardId);
            }
        }

        applyData.newHandCount = authoritativeHand.Count;
        applyData.remainingDeckCount = deckCardIds.Count;

        if (applyData.affectedPlayerHandCounts == null || applyData.affectedPlayerHandCounts.Count == 0)
        {
            applyData.affectedPlayerHandCounts = BuildAffectedHandCounts(request.actorPlayerId);
        }
    }

    private void BroadcastApplyToAll(OnlineCardApplyData applyData)
    {
        if (!CanRunOnlineGameSceneCardSync())
        {
            return;
        }

        PhotonNetwork.RaiseEvent(
            CardSyncApplyEventCode,
            JsonUtility.ToJson(applyData),
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    private void RequestPrivateHandStateFromMaster()
    {
        if (!CanRunOnlineGameSceneCardSync() ||
            PhotonNetwork.IsMasterClient ||
            onlineGameSceneManager == null ||
            onlineGameSceneManager.LocalPlayerId < 0)
        {
            return;
        }

        object[] requestPayload = { onlineGameSceneManager.LocalPlayerId };

        Debug.Log(
            "Requesting private online hand state" +
            " ownerPlayerId=" + onlineGameSceneManager.LocalPlayerId +
            " actorNumber=" + (PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1)
        );

        PhotonNetwork.RaiseEvent(
            CardSyncPrivateHandRequestEventCode,
            requestPayload,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );
    }

    private void ProcessPrivateHandStateRequest(EventData photonEvent)
    {
        if (!CanRunOnlineGameSceneCardSync() || !PhotonNetwork.IsMasterClient)
        {
            return;
        }

        int requestedPlayerId = ExtractRequestedPrivateHandPlayerId(photonEvent.CustomData);
        int senderPlayerId = ResolvePlayerIdForActorNumber(photonEvent.Sender);

        if (requestedPlayerId < 0)
        {
            requestedPlayerId = senderPlayerId;
        }

        if (requestedPlayerId < 0 || senderPlayerId != requestedPlayerId)
        {
            Debug.LogWarning(
                "Rejected private hand state request" +
                " senderActor=" + photonEvent.Sender +
                " requestedPlayerId=" + requestedPlayerId +
                " senderPlayerId=" + senderPlayerId
            );
            return;
        }

        Debug.Log(
            "Accepted private hand state request" +
            " senderActor=" + photonEvent.Sender +
            " requestedPlayerId=" + requestedPlayerId
        );
        SendPrivateHandStateToOwner(requestedPlayerId);
    }

    private int ExtractRequestedPrivateHandPlayerId(object requestData)
    {
        if (requestData is object[] requestArray && requestArray.Length > 0)
        {
            return ConvertToInt(requestArray[0], -1);
        }

        return ConvertToInt(requestData, -1);
    }

    private void SendApplyToRequester(int targetActorNumber, OnlineCardApplyData applyData)
    {
        if (!CanRunOnlineGameSceneCardSync() || targetActorNumber < 0)
        {
            return;
        }

        PhotonNetwork.RaiseEvent(
            CardSyncApplyEventCode,
            JsonUtility.ToJson(applyData),
            new RaiseEventOptions { TargetActors = new[] { targetActorNumber } },
            new SendOptions { Reliability = true }
        );
    }

    private void SendPrivateHandStateToOwner(int ownerPlayerId)
    {
        if (!CanRunOnlineGameSceneCardSync())
        {
            return;
        }

        int targetActorNumber = ResolveActorNumberForPlayerId(ownerPlayerId);

        if (targetActorNumber < 0)
        {
            return;
        }

        OnlinePrivateHandStateData handState = BuildPrivateHandState(ownerPlayerId);
        string payload = JsonUtility.ToJson(handState);

        Debug.Log(
            "Online private hand update recipient" +
            " ownerPlayerId=" + ownerPlayerId +
            " targetActorNumber=" + targetActorNumber +
            " handCount=" + handState.handCount
        );

        if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == targetActorNumber)
        {
            ApplyPrivateHandState(handState);
        }

        PhotonNetwork.RaiseEvent(
            CardSyncPrivateHandEventCode,
            payload,
            new RaiseEventOptions { TargetActors = new[] { targetActorNumber } },
            new SendOptions { Reliability = true }
        );
    }

    private void BootstrapAuthoritativeStateAsMaster()
    {
        if (!CanRunOnlineGameSceneCardSync())
        {
            return;
        }

        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PhotonLobbyPropertyKeys.OnlineCardPublicSnapshot))
        {
            if (deckCardIds.Count <= 0)
            {
                Debug.LogWarning("Online card snapshot exists, but master runtime card state is empty. Reinitializing from local deck only.");
            }
            else
            {
                ApplySnapshotFromRoomProperties();
                SendPrivateHandStateToAllActivePlayers();
                return;
            }
        }

        deckCardIds.Clear();
        playerHandCardIds.Clear();

        List<string> configuredDeck = cardDrawManager.BuildConfiguredDeckCardIds();
        deckCardIds.AddRange(configuredDeck);
        ShuffleDeckCardIds();

        foreach (PlayerResource player in GetActivePlayers())
        {
            List<string> hand = GetOrCreateAuthoritativeHand(player.playerId);
            hand.Clear();

            int handLimit = cardDrawManager.GetMaxHandSizeForPlayer(player.playerId);
            int drawCount = Mathf.Min(cardDrawManager.initialCardsPerPlayer, handLimit);

            for (int i = 0; i < drawCount && deckCardIds.Count > 0; i++)
            {
                hand.Add(deckCardIds[0]);
                deckCardIds.RemoveAt(0);
            }
        }

        UpdateSnapshotProperty();
        SendPrivateHandStateToAllActivePlayers();
    }

    private void SendPrivateHandStateToAllActivePlayers()
    {
        foreach (PlayerResource player in GetActivePlayers())
        {
            SendPrivateHandStateToOwner(player.playerId);
        }
    }

    private void ApplySnapshotFromRoomProperties()
    {
        if (!CanRunOnlineGameSceneCardSync() ||
            PhotonNetwork.CurrentRoom == null ||
            PhotonNetwork.CurrentRoom.CustomProperties == null ||
            !PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PhotonLobbyPropertyKeys.OnlineCardPublicSnapshot))
        {
            return;
        }

        string snapshotJson = PhotonNetwork.CurrentRoom.CustomProperties[PhotonLobbyPropertyKeys.OnlineCardPublicSnapshot] as string;
        OnlineCardPublicSnapshot snapshot = string.IsNullOrWhiteSpace(snapshotJson)
            ? null
            : JsonUtility.FromJson<OnlineCardPublicSnapshot>(snapshotJson);

        if (snapshot == null || playerManager == null)
        {
            return;
        }

        Dictionary<int, int> incomingCounts = BuildHandCountMap(snapshot);

        foreach (PlayerResource player in playerManager.players)
        {
            if (player == null)
            {
                continue;
            }

            if (incomingCounts.TryGetValue(player.playerId, out int handCount))
            {
                player.SetCardCount(handCount);
                continue;
            }

            if (player.isEliminated || IsKnownEmptyPlayerSlot(player))
            {
                player.SetCardCount(0);
                continue;
            }

            Debug.LogWarning(
                "Online card snapshot missing playerId=" + player.playerId +
                ". Preserving existing public count=" + player.cardCount + "."
            );
        }

        playerManager.RefreshAllPlayerStatusPanels();
        playerManager.RefreshCurrentPlayerUI();
        Debug.Log("Applied public online card snapshot: " + FormatSnapshotCounts(snapshot));
    }

    private void UpdateSnapshotProperty()
    {
        if (!CanRunOnlineGameSceneCardSync() || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        OnlineCardPublicSnapshot snapshot = BuildPublicSnapshot();
        PhotonHashtable properties = new PhotonHashtable
        {
            { PhotonLobbyPropertyKeys.OnlineCardPublicSnapshot, JsonUtility.ToJson(snapshot) }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
    }

    private OnlineCardPublicSnapshot BuildPublicSnapshot()
    {
        OnlineCardPublicSnapshot snapshot = new OnlineCardPublicSnapshot
        {
            remainingDeckCount = deckCardIds.Count,
            playerHandCounts = new List<OnlinePlayerHandCountState>()
        };

        if (playerManager == null || playerManager.players == null)
        {
            return snapshot;
        }

        foreach (PlayerResource player in playerManager.players)
        {
            if (player == null)
            {
                continue;
            }

            int handCount = 0;

            if (!player.isEliminated && !IsKnownEmptyPlayerSlot(player))
            {
                List<string> hand = GetOrCreateAuthoritativeHand(player.playerId);
                handCount = hand.Count;
            }

            snapshot.playerHandCounts.Add(new OnlinePlayerHandCountState
            {
                playerId = player.playerId,
                handCount = handCount
            });
        }

        return snapshot;
    }
#endif

    private void ApplyConfirmedAction(OnlineCardApplyData applyData)
    {
        AutoAssignReferences();

        if (applyData == null || onlineGameSceneManager == null)
        {
            return;
        }

        if (!applyData.accepted)
        {
            if (onlineGameSceneManager.IsLocalOnlinePlayer(applyData.actorPlayerId))
            {
                if (!string.IsNullOrWhiteSpace(applyData.rejectReason))
                {
                    onlineGameSceneManager.ShowOnlineToast(applyData.rejectReason);
                }

                if (cardDrawManager != null)
                {
                    cardDrawManager.CancelPendingCard();
                }
            }

            return;
        }

        ConsumeLocalTurnState(applyData);
        ApplySyncedTileState(applyData);

        if (playerManager != null)
        {
            if (applyData.affectedPlayerHandCounts != null && applyData.affectedPlayerHandCounts.Count > 0)
            {
                for (int i = 0; i < applyData.affectedPlayerHandCounts.Count; i++)
                {
                    OnlinePlayerHandCountState affectedState = applyData.affectedPlayerHandCounts[i];

                    if (affectedState == null)
                    {
                        continue;
                    }

                    PlayerResource affectedPlayer = playerManager.GetPlayerResource(affectedState.playerId);

                    if (affectedPlayer != null)
                    {
                        affectedPlayer.SetCardCount(Mathf.Max(0, affectedState.handCount));
                    }
                }
            }
            else
            {
                PlayerResource actor = playerManager.GetPlayerResource(applyData.actorPlayerId);

                if (actor != null)
                {
                    actor.SetCardCount(applyData.newHandCount);
                }
            }

            if (applyData.actorNewGold >= 0)
            {
                PlayerResource actor = playerManager.GetPlayerResource(applyData.actorPlayerId);

                if (actor != null)
                {
                    actor.money = applyData.actorNewGold;
                }
            }

            if (applyData.targetDisrupted && applyData.targetPlayerId >= 0)
            {
                PlayerResource disruptedTarget = playerManager.GetPlayerResource(applyData.targetPlayerId);

                if (disruptedTarget != null)
                {
                    disruptedTarget.ApplyDisruptNextTurn();
                }
            }

            playerManager.RefreshAllPlayerStatusPanels();
            playerManager.RefreshCurrentPlayerUI();
        }

        ShowActionToast(applyData);
    }

    private void ApplyPrivateHandState(OnlinePrivateHandStateData privateState)
    {
        AutoAssignReferences();

        if (cardDrawManager == null || privateState == null)
        {
            return;
        }

        cardDrawManager.ApplyOnlinePrivateHandState(
            privateState.ownerPlayerId,
            privateState.cardIds,
            privateState.handCount
        );

        if (onlineGameSceneManager != null &&
            onlineGameSceneManager.IsLocalOnlinePlayer(privateState.ownerPlayerId))
        {
            Debug.Log(
                "Applied private online hand state for local owner playerId=" + privateState.ownerPlayerId +
                ", handCount=" + privateState.handCount +
                ", cardIds=[" + string.Join(",", privateState.cardIds != null ? privateState.cardIds.ToArray() : new string[0]) + "]"
            );
        }
    }

    private void ApplySyncedTileState(OnlineCardApplyData applyData)
    {
        if (applyData == null || !IsLandControlCardId(applyData.cardId))
        {
            return;
        }

        ApplyTileStateToScene(applyData);

        Debug.Log(
            (IsTakeOverCardId(applyData.cardId) ? "ApplyTakeOver" : "ApplyFreezeClaim") +
            " actorPlayerId=" + applyData.actorPlayerId +
            " targetTileId=" + applyData.targetTileId +
            " previousOwner=" + applyData.previousOwnerPlayerId +
            " newOwner=" + applyData.newOwnerPlayerId +
            " accepted=true rejectedReason=(none)"
        );
    }

    private void ApplyTileStateToScene(OnlineCardApplyData applyData)
    {
        if (playerManager == null)
        {
            playerManager = FindObjectOfType<PlayerManager>();
        }

        TowerBuildArea targetTile = ResolveTileTargetById(applyData.targetTileId);

        if (targetTile == null)
        {
            Debug.LogWarning("Online card tile apply failed. targetTileId=" + applyData.targetTileId);
            return;
        }

        if (applyData.removeTowerFromTile)
        {
            targetTile.RemoveCurrentTower();
        }

        if (IsTakeOverCardId(applyData.cardId) && applyData.newOwnerPlayerId >= 0)
        {
            targetTile.ownerPlayerId = applyData.newOwnerPlayerId;
            targetTile.isOwned = true;
            targetTile.inactiveForPlayerId = applyData.newOwnerPlayerId;
            targetTile.activatesNextTurn = true;
        }
        else if (IsTakeOverCardId(applyData.cardId) && applyData.previousOwnerPlayerId < 0)
        {
            targetTile.ClearOwner();
        }

        if (applyData.tileFrozen)
        {
            targetTile.FreezeForPlayer(applyData.frozenByPlayerId);
            targetTile.frozenUntilPlayerNextTurn = applyData.frozenUntilPlayerNextTurn;
        }
        else
        {
            targetTile.ClearFreeze();
        }

        targetTile.RefreshOwnershipVisual(playerManager);

        Debug.Log(
            "Applied online tile state" +
            " targetTileId=" + applyData.targetTileId +
            " actualOwner=" + targetTile.ownerPlayerId +
            " actualOwned=" + targetTile.isOwned +
            " actualFrozen=" + targetTile.isFrozenOrSealed +
            " frozenBy=" + targetTile.frozenByPlayerId
        );
    }

    private void UpdateBuildSnapshotForTileState(OnlineCardApplyData applyData)
    {
#if PHOTON_UNITY_NETWORKING
        if (!PhotonNetwork.IsMasterClient || applyData == null)
        {
            return;
        }

        PhotonOnlineBuildSyncManager buildSyncManager = PhotonOnlineBuildSyncManager.Instance != null
            ? PhotonOnlineBuildSyncManager.Instance
            : FindObjectOfType<PhotonOnlineBuildSyncManager>();

        if (buildSyncManager == null)
        {
            return;
        }

        buildSyncManager.UpdateOnlineTileStateSnapshot(
            applyData.targetTileId,
            applyData.newOwnerPlayerId >= 0,
            applyData.newOwnerPlayerId,
            applyData.tileFrozen,
            applyData.frozenByPlayerId,
            applyData.frozenUntilPlayerNextTurn,
            applyData.removeTowerFromTile
        );
#endif
    }

    private void ConsumeLocalTurnState(OnlineCardApplyData applyData)
    {
        if (turnManager == null || onlineGameSceneManager == null)
        {
            return;
        }

        if (onlineGameSceneManager.CurrentTurnPlayerId != applyData.actorPlayerId)
        {
            return;
        }

        if (applyData.actionType == OnlineCardActionType.Draw)
        {
            turnManager.ApplyAuthoritativeDrawConsumed();
        }
        else if (applyData.actionType == OnlineCardActionType.Discard)
        {
            turnManager.ApplyAuthoritativeDiscardConsumed();
        }
        else if (applyData.actionType == OnlineCardActionType.Play)
        {
            turnManager.ApplyAuthoritativePlayConsumed(1);
        }

        Debug.Log(
            "Applied authoritative turn card state" +
            " actionType=" + applyData.actionType +
            " currentPlayerId=" + turnManager.currentPlayerId +
            " hasDrawnCard=" + turnManager.hasDrawnCard +
            " hasPlayedCard=" + turnManager.hasPlayedCard +
            " hasDiscardedCard=" + turnManager.hasDiscardedCard +
            " currentAP=" + turnManager.currentAP
        );
    }

    private void ShowActionToast(OnlineCardApplyData applyData)
    {
        if (onlineGameSceneManager == null)
        {
            return;
        }

        bool isLocalActor = onlineGameSceneManager.IsLocalOnlinePlayer(applyData.actorPlayerId);
        string actorName = playerManager != null
            ? playerManager.GetPlayerDisplayName(applyData.actorPlayerId)
            : "Player " + applyData.actorPlayerId;

        if (applyData.actionType == OnlineCardActionType.Draw)
        {
            onlineGameSceneManager.ShowOnlineToast(isLocalActor ? "You drew a card." : actorName + " drew a card.");
            return;
        }

        if (applyData.actionType == OnlineCardActionType.Discard)
        {
            onlineGameSceneManager.ShowOnlineToast(isLocalActor ? "You discarded a card." : actorName + " discarded a card.");
            return;
        }

        string normalizedCardId = CardDrawManager.NormalizeCardId(applyData.cardId);
        bool isLocalTarget = onlineGameSceneManager.IsLocalOnlinePlayer(applyData.targetPlayerId);
        string targetName = playerManager != null
            ? playerManager.GetPlayerDisplayName(applyData.targetPlayerId)
            : "Player " + applyData.targetPlayerId;

        if (IsStealCardId(normalizedCardId))
        {
            onlineGameSceneManager.ShowOnlineToast(
                isLocalActor
                    ? "You stole a card from " + targetName + "."
                    : isLocalTarget
                        ? actorName + " stole a card from you."
                        : actorName + " stole a card from " + targetName + "."
            );
            return;
        }

        if (IsTradeHandsCardId(normalizedCardId))
        {
            onlineGameSceneManager.ShowOnlineToast(
                isLocalActor
                    ? "You traded hands with " + targetName + "."
                    : isLocalTarget
                        ? actorName + " traded hands with you."
                        : actorName + " traded hands with " + targetName + "."
            );
            return;
        }

        if (IsDisruptCardId(normalizedCardId))
        {
            onlineGameSceneManager.ShowOnlineToast(
                isLocalActor
                    ? "You disrupted " + targetName + "."
                    : isLocalTarget
                        ? "You were disrupted by " + actorName + "."
                        : actorName + " disrupted " + targetName + "."
            );
            return;
        }

        if (IsTakeOverCardId(normalizedCardId))
        {
            bool isLocalPreviousOwner = onlineGameSceneManager.IsLocalOnlinePlayer(applyData.previousOwnerPlayerId);
            onlineGameSceneManager.ShowOnlineToast(
                isLocalActor
                    ? "You took over a tile."
                    : isLocalPreviousOwner
                        ? actorName + " took over one of your tiles."
                        : actorName + " took over a tile."
            );
            return;
        }

        if (IsFreezeClaimCardId(normalizedCardId))
        {
            bool isLocalTileOwner = onlineGameSceneManager.IsLocalOnlinePlayer(applyData.previousOwnerPlayerId);
            onlineGameSceneManager.ShowOnlineToast(
                isLocalActor
                    ? "You froze a tile."
                    : isLocalTileOwner
                        ? actorName + " froze one of your tiles."
                        : actorName + " froze a tile."
            );
            return;
        }

        string playedCardName = string.IsNullOrWhiteSpace(applyData.cardName) ? "a card" : applyData.cardName;
        onlineGameSceneManager.ShowOnlineToast(isLocalActor
            ? "You played " + playedCardName + "."
            : actorName + " played " + playedCardName + ".");
    }

    private void AutoAssignReferences()
    {
        if (playerManager == null)
        {
            playerManager = FindObjectOfType<PlayerManager>();
        }

        if (cardDrawManager == null)
        {
            cardDrawManager = FindObjectOfType<CardDrawManager>();
        }

        if (turnManager == null)
        {
            turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindObjectOfType<TurnManager>();
        }

        if (onlineGameSceneManager == null)
        {
            onlineGameSceneManager = PhotonOnlineGameSceneManager.Instance != null
                ? PhotonOnlineGameSceneManager.Instance
                : FindObjectOfType<PhotonOnlineGameSceneManager>();
        }

        RebuildTileTargetRegistryIfNeeded();
    }

    private TowerBuildArea ResolveTileTargetById(string targetTileId)
    {
        RebuildTileTargetRegistryIfNeeded();

        if (string.IsNullOrWhiteSpace(targetTileId))
        {
            return null;
        }

        tileTargetsById.TryGetValue(targetTileId, out TowerBuildArea targetTile);
        return targetTile;
    }

    private void RebuildTileTargetRegistryIfNeeded()
    {
        if (tileTargetsById.Count > 0)
        {
            return;
        }

        RebuildTileTargetRegistry();
    }

    private void RebuildTileTargetRegistry()
    {
        tileTargetsById.Clear();
        TowerBuildArea[] buildAreas = FindObjectsOfType<TowerBuildArea>(true);

        foreach (TowerBuildArea buildArea in buildAreas)
        {
            if (buildArea == null || string.IsNullOrWhiteSpace(buildArea.name))
            {
                continue;
            }

            if (tileTargetsById.ContainsKey(buildArea.name))
            {
                Debug.LogWarning("Duplicate online tile target id found: " + buildArea.name);
                continue;
            }

            tileTargetsById.Add(buildArea.name, buildArea);
        }
    }

    private List<PlayerResource> GetActivePlayers()
    {
        List<PlayerResource> players = new List<PlayerResource>();

        if (playerManager == null || playerManager.players == null)
        {
            return players;
        }

        foreach (PlayerResource player in playerManager.players)
        {
            if (player != null && !player.isEliminated)
            {
                players.Add(player);
            }
        }

        players.Sort((left, right) => left.playerId.CompareTo(right.playerId));
        return players;
    }

    private List<string> GetOrCreateAuthoritativeHand(int playerId)
    {
        if (!playerHandCardIds.TryGetValue(playerId, out List<string> hand))
        {
            hand = new List<string>();
            playerHandCardIds[playerId] = hand;
        }

        return hand;
    }

    private bool HandContainsCardId(List<string> hand, string cardId)
    {
        if (hand == null)
        {
            return false;
        }

        for (int i = 0; i < hand.Count; i++)
        {
            if (CardIdsMatch(hand[i], cardId))
            {
                return true;
            }
        }

        return false;
    }

    private bool RemoveCardIdFromHand(List<string> hand, string cardId)
    {
        string removedCardId;
        return RemoveCardIdFromHand(hand, cardId, out removedCardId);
    }

    private bool RemoveCardIdFromHand(List<string> hand, string cardId, out string removedCardId)
    {
        removedCardId = string.Empty;

        if (hand == null)
        {
            return false;
        }

        for (int i = 0; i < hand.Count; i++)
        {
            if (!CardIdsMatch(hand[i], cardId))
            {
                continue;
            }

            removedCardId = hand[i];
            hand.RemoveAt(i);
            return true;
        }

        return false;
    }

    private bool CardIdsMatch(string leftCardId, string rightCardId)
    {
        return CanonicalCardId(leftCardId) == CanonicalCardId(rightCardId);
    }

    private string CanonicalCardId(string cardId)
    {
        return CardDrawManager.NormalizeCardId(cardId).Replace(" ", string.Empty);
    }

    private OnlinePrivateHandStateData BuildPrivateHandState(int ownerPlayerId)
    {
        List<string> hand = GetOrCreateAuthoritativeHand(ownerPlayerId);
        return new OnlinePrivateHandStateData
        {
            ownerPlayerId = ownerPlayerId,
            cardIds = new List<string>(hand),
            handCount = hand.Count,
            remainingDeckCount = deckCardIds.Count
        };
    }

    private void ShuffleDeckCardIds()
    {
        for (int i = 0; i < deckCardIds.Count; i++)
        {
            int randomIndex = Random.Range(i, deckCardIds.Count);
            string temp = deckCardIds[i];
            deckCardIds[i] = deckCardIds[randomIndex];
            deckCardIds[randomIndex] = temp;
        }
    }

    private bool HasValidTargetMetadata(string cardId, string targetId, int targetPlayerId, int actorPlayerId)
    {
        string normalizedCardId = CardDrawManager.NormalizeCardId(cardId);

        if (CardDrawManager.IsPlayerTargetingCardId(normalizedCardId))
        {
            return targetPlayerId >= 0 && targetPlayerId != actorPlayerId;
        }

        if (CardDrawManager.RequiresBoardTargetId(normalizedCardId))
        {
            return !string.IsNullOrWhiteSpace(targetId);
        }

        return true;
    }

    private OnlineCardApplyData RejectValidation(OnlineCardRequestData request, OnlineCardApplyData result, string rejectReason)
    {
        result.rejectReason = rejectReason;
        LogCardEffectValidation(request, result);

        if (request != null && result != null)
        {
            if (IsTakeOverCardId(request.cardId))
            {
                LogLandControlValidation("ValidateTakeOver", request, result, rejectReason);
            }
            else if (IsFreezeClaimCardId(request.cardId))
            {
                LogLandControlValidation("ValidateFreezeClaim", request, result, rejectReason);
            }
        }

        return result;
    }

    private string ValidatePlayerInteractionCardEffect(OnlineCardRequestData request)
    {
        if (request == null)
        {
            return "Invalid request.";
        }

        if (!IsActiveTargetPlayer(request.targetPlayerId))
        {
            return "Target player is not active.";
        }

        if (request.targetPlayerId == request.actorPlayerId)
        {
            return "Cannot target yourself.";
        }

        string normalizedCardId = CardDrawManager.NormalizeCardId(request.cardId);
        List<string> targetHand = GetOrCreateAuthoritativeHand(request.targetPlayerId);

        if (IsStealCardId(normalizedCardId) && targetHand.Count <= 0)
        {
            return "Target has no cards.";
        }

        if (IsDisruptCardId(normalizedCardId))
        {
            PlayerResource targetPlayer = playerManager != null
                ? playerManager.GetPlayerResource(request.targetPlayerId)
                : null;

            if (targetPlayer == null)
            {
                return "Target player is missing.";
            }

            if (targetPlayer.HasPendingDisrupt())
            {
                return "This player is already disrupted.";
            }
        }

        return string.Empty;
    }

    private bool IsActiveTargetPlayer(int playerId)
    {
        if (playerManager == null || playerId < 0)
        {
            return false;
        }

        PlayerResource targetPlayer = playerManager.GetPlayerResource(playerId);
        return targetPlayer != null && !targetPlayer.isEliminated && !IsKnownEmptyPlayerSlot(targetPlayer);
    }

    private bool IsPlayerInteractionCardId(string cardId)
    {
        return IsStealCardId(cardId) || IsTradeHandsCardId(cardId) || IsDisruptCardId(cardId);
    }

    private bool IsLandControlCardId(string cardId)
    {
        return IsTakeOverCardId(cardId) || IsFreezeClaimCardId(cardId);
    }

    private bool IsStealCardId(string cardId)
    {
        string normalizedCardId = CardDrawManager.NormalizeCardId(cardId);
        return normalizedCardId == "stealcard" || normalizedCardId == "steal card";
    }

    private bool IsTradeHandsCardId(string cardId)
    {
        string normalizedCardId = CardDrawManager.NormalizeCardId(cardId);
        return normalizedCardId == "tradehands" || normalizedCardId == "trade hands";
    }

    private bool IsDisruptCardId(string cardId)
    {
        return CardDrawManager.NormalizeCardId(cardId) == "disrupt";
    }

    private bool IsTakeOverCardId(string cardId)
    {
        string normalizedCardId = CardDrawManager.NormalizeCardId(cardId);
        return normalizedCardId == "takeover" || normalizedCardId == "take over";
    }

    private bool IsFreezeClaimCardId(string cardId)
    {
        string normalizedCardId = CardDrawManager.NormalizeCardId(cardId);
        return normalizedCardId == "freezeclaim" || normalizedCardId == "freeze claim";
    }

    private string ValidateLandControlCardEffect(OnlineCardRequestData request, OnlineCardApplyData result)
    {
        if (request == null)
        {
            return "Invalid request.";
        }

        TowerBuildArea targetTile = ResolveTileTargetById(request.targetId);

        if (targetTile == null)
        {
            return "Target tile is missing.";
        }

        result.targetTileId = request.targetId;
        result.previousOwnerPlayerId = targetTile.ownerPlayerId;
        result.newOwnerPlayerId = targetTile.ownerPlayerId;
        result.tileFrozen = targetTile.isFrozenOrSealed;
        result.frozenByPlayerId = targetTile.frozenByPlayerId;
        result.frozenUntilPlayerNextTurn = targetTile.frozenUntilPlayerNextTurn;

        if (IsTakeOverCardId(request.cardId))
        {
            if (!targetTile.isOwned || targetTile.ownerPlayerId < 0)
            {
                return "Target tile has no owner.";
            }

            if (targetTile.ownerPlayerId == request.actorPlayerId)
            {
                return "Choose an opponent-owned land.";
            }

            if (targetTile.isFrozenOrSealed || !targetTile.CanBeTakenOverBy(request.actorPlayerId))
            {
                return "This land cannot be taken over.";
            }

            PlayerResource actor = playerManager != null
                ? playerManager.GetPlayerResource(request.actorPlayerId)
                : null;

            if (actor == null)
            {
                return "Player resource is missing.";
            }

            int takeOverCost = targetTile.GetTakeOverCardCost();

            if (!actor.CanAfford(takeOverCost))
            {
                return "Not enough gold to take over this land.";
            }

            result.newOwnerPlayerId = request.actorPlayerId;
            result.actorNewGold = actor.money - takeOverCost;
            return string.Empty;
        }

        if (IsFreezeClaimCardId(request.cardId))
        {
            if (targetTile.isFrozenOrSealed || !targetTile.CanBeFrozen())
            {
                return "This land is already frozen.";
            }

            result.tileFrozen = true;
            result.frozenByPlayerId = request.actorPlayerId;
            result.frozenUntilPlayerNextTurn = true;
            return string.Empty;
        }

        return "Unsupported land-control card.";
    }

    private void ApplyStealCardEffect(
        OnlineCardRequestData request,
        List<string> actorHand,
        OnlineCardApplyData applyData)
    {
        List<string> targetHand = GetOrCreateAuthoritativeHand(request.targetPlayerId);
        RemoveCardIdFromHand(actorHand, request.cardId);

        int randomIndex = Random.Range(0, targetHand.Count);
        string stolenCardId = targetHand[randomIndex];
        targetHand.RemoveAt(randomIndex);
        actorHand.Add(stolenCardId);

        applyData.affectedPlayerHandCounts = BuildAffectedHandCounts(request.actorPlayerId, request.targetPlayerId);

        Debug.Log(
            "ApplyStealCard" +
            " actorPlayerId=" + request.actorPlayerId +
            " targetPlayerId=" + request.targetPlayerId +
            " cardId=" + request.cardId +
            " cardName=" + request.cardName +
            " stolenCardId=" + stolenCardId
        );
    }

    private void ApplyTradeHandsEffect(
        OnlineCardRequestData request,
        List<string> actorHand,
        OnlineCardApplyData applyData)
    {
        List<string> targetHand = GetOrCreateAuthoritativeHand(request.targetPlayerId);
        RemoveCardIdFromHand(actorHand, request.cardId);

        List<string> actorRemainingHand = new List<string>(actorHand);
        List<string> targetOriginalHand = new List<string>(targetHand);

        actorHand.Clear();
        actorHand.AddRange(targetOriginalHand);
        targetHand.Clear();
        targetHand.AddRange(actorRemainingHand);

        applyData.affectedPlayerHandCounts = BuildAffectedHandCounts(request.actorPlayerId, request.targetPlayerId);

        Debug.Log(
            "ApplyTradeHands" +
            " actorPlayerId=" + request.actorPlayerId +
            " targetPlayerId=" + request.targetPlayerId +
            " cardId=" + request.cardId +
            " cardName=" + request.cardName +
            " actorNewHandCount=" + actorHand.Count +
            " targetNewHandCount=" + targetHand.Count
        );
    }

    private void ApplyDisruptEffect(
        OnlineCardRequestData request,
        List<string> actorHand,
        OnlineCardApplyData applyData)
    {
        RemoveCardIdFromHand(actorHand, request.cardId);
        applyData.targetDisrupted = true;
        applyData.affectedPlayerHandCounts = BuildAffectedHandCounts(request.actorPlayerId, request.targetPlayerId);

        PlayerResource targetPlayer = playerManager != null
            ? playerManager.GetPlayerResource(request.targetPlayerId)
            : null;

        if (targetPlayer != null)
        {
            targetPlayer.ApplyDisruptNextTurn();
        }

        Debug.Log(
            "ApplyDisrupt" +
            " actorPlayerId=" + request.actorPlayerId +
            " targetPlayerId=" + request.targetPlayerId +
            " cardId=" + request.cardId +
            " cardName=" + request.cardName
        );
    }

    private void ApplyTakeOverEffect(
        OnlineCardRequestData request,
        List<string> actorHand,
        OnlineCardApplyData applyData)
    {
        TowerBuildArea targetTile = ResolveTileTargetById(request.targetId);

        if (targetTile == null)
        {
            applyData.accepted = false;
            applyData.rejectReason = "Target tile is missing.";
            return;
        }

        RemoveCardIdFromHand(actorHand, request.cardId);

        PlayerResource actor = playerManager != null
            ? playerManager.GetPlayerResource(request.actorPlayerId)
            : null;

        if (actor != null && applyData.actorNewGold >= 0)
        {
            actor.money = applyData.actorNewGold;
        }

        applyData.targetTileId = request.targetId;
        applyData.previousOwnerPlayerId = targetTile.ownerPlayerId;
        applyData.newOwnerPlayerId = request.actorPlayerId;
        applyData.tileFrozen = false;
        applyData.frozenByPlayerId = -1;
        applyData.frozenUntilPlayerNextTurn = false;
        applyData.removeTowerFromTile = true;
        applyData.targetPlayerId = applyData.previousOwnerPlayerId;
        applyData.affectedPlayerHandCounts = BuildAffectedHandCounts(request.actorPlayerId);

        ApplyTileStateToScene(applyData);
        UpdateBuildSnapshotForTileState(applyData);

        Debug.Log(
            "ApplyTakeOver" +
            " actorPlayerId=" + request.actorPlayerId +
            " targetTileId=" + request.targetId +
            " previousOwner=" + applyData.previousOwnerPlayerId +
            " newOwner=" + applyData.newOwnerPlayerId +
            " accepted=true rejectedReason=(none)"
        );
    }

    private void ApplyFreezeClaimEffect(
        OnlineCardRequestData request,
        List<string> actorHand,
        OnlineCardApplyData applyData)
    {
        TowerBuildArea targetTile = ResolveTileTargetById(request.targetId);

        if (targetTile == null)
        {
            applyData.accepted = false;
            applyData.rejectReason = "Target tile is missing.";
            return;
        }

        RemoveCardIdFromHand(actorHand, request.cardId);

        applyData.targetTileId = request.targetId;
        applyData.previousOwnerPlayerId = targetTile.ownerPlayerId;
        applyData.newOwnerPlayerId = targetTile.ownerPlayerId;
        applyData.tileFrozen = true;
        applyData.frozenByPlayerId = request.actorPlayerId;
        applyData.frozenUntilPlayerNextTurn = true;
        applyData.removeTowerFromTile = false;
        applyData.targetPlayerId = applyData.previousOwnerPlayerId;
        applyData.affectedPlayerHandCounts = BuildAffectedHandCounts(request.actorPlayerId);

        ApplyTileStateToScene(applyData);
        UpdateBuildSnapshotForTileState(applyData);

        Debug.Log(
            "ApplyFreezeClaim" +
            " actorPlayerId=" + request.actorPlayerId +
            " targetTileId=" + request.targetId +
            " previousOwner=" + applyData.previousOwnerPlayerId +
            " newOwner=" + applyData.newOwnerPlayerId +
            " accepted=true rejectedReason=(none)"
        );
    }

    private List<OnlinePlayerHandCountState> BuildAffectedHandCounts(params int[] playerIds)
    {
        List<OnlinePlayerHandCountState> affectedCounts = new List<OnlinePlayerHandCountState>();

        if (playerIds == null)
        {
            return affectedCounts;
        }

        for (int i = 0; i < playerIds.Length; i++)
        {
            AddAffectedHandCountState(affectedCounts, playerIds[i]);
        }

        return affectedCounts;
    }

    private void AddAffectedHandCountState(List<OnlinePlayerHandCountState> affectedCounts, int playerId)
    {
        if (affectedCounts == null || playerId < 0)
        {
            return;
        }

        for (int i = 0; i < affectedCounts.Count; i++)
        {
            if (affectedCounts[i] != null && affectedCounts[i].playerId == playerId)
            {
                return;
            }
        }

        affectedCounts.Add(new OnlinePlayerHandCountState
        {
            playerId = playerId,
            handCount = GetOrCreateAuthoritativeHand(playerId).Count
        });
    }

    private void SendPrivateHandStatesForAcceptedAction(OnlineCardApplyData applyData)
    {
        if (applyData == null || !applyData.accepted)
        {
            return;
        }

        SendPrivateHandStateToOwner(applyData.actorPlayerId);

        if ((IsStealCardId(applyData.cardId) || IsTradeHandsCardId(applyData.cardId)) &&
            applyData.targetPlayerId >= 0 &&
            applyData.targetPlayerId != applyData.actorPlayerId)
        {
            SendPrivateHandStateToOwner(applyData.targetPlayerId);
        }
    }

    private void LogCardEffectValidation(OnlineCardRequestData request, OnlineCardApplyData applyData)
    {
        if (request == null || applyData == null || !IsPlayerInteractionCardId(request.cardId))
        {
            return;
        }

        Debug.Log(
            "ValidateCardEffect" +
            " actorPlayerId=" + request.actorPlayerId +
            " targetPlayerId=" + request.targetPlayerId +
            " cardId=" + request.cardId +
            " cardName=" + request.cardName +
            " accepted=" + applyData.accepted +
            " rejectReason=" + (string.IsNullOrWhiteSpace(applyData.rejectReason) ? "(none)" : applyData.rejectReason)
        );
    }

    private void LogLandControlValidation(string label, OnlineCardRequestData request, OnlineCardApplyData applyData, string reason)
    {
        if (request == null || applyData == null)
        {
            return;
        }

        Debug.Log(
            label +
            " actorPlayerId=" + request.actorPlayerId +
            " targetTileId=" + request.targetId +
            " previousOwner=" + applyData.previousOwnerPlayerId +
            " newOwner=" + applyData.newOwnerPlayerId +
            " accepted=" + applyData.accepted +
            " rejectedReason=" + (string.IsNullOrWhiteSpace(reason) ? "(none)" : reason)
        );
    }

    private int ResolveActorNumberForPlayerId(int playerId)
    {
#if !PHOTON_UNITY_NETWORKING
        return -1;
#else
        if (!CanRunOnlineGameSceneCardSync())
        {
            return -1;
        }

        foreach (Player photonPlayer in PhotonNetwork.PlayerList)
        {
            if (onlineGameSceneManager != null &&
                onlineGameSceneManager.GetOnlinePlayerIdByActorNumber(photonPlayer.ActorNumber) == playerId)
            {
                return photonPlayer.ActorNumber;
            }

            if (GetPhotonPlayerIntProperty(photonPlayer, PhotonLobbyPropertyKeys.PlayerId, -1) == playerId)
            {
                return photonPlayer.ActorNumber;
            }
        }

        return -1;
#endif
    }

    private int ResolvePlayerIdForActorNumber(int actorNumber)
    {
#if !PHOTON_UNITY_NETWORKING
        return -1;
#else
        if (!CanRunOnlineGameSceneCardSync())
        {
            return -1;
        }

        if (onlineGameSceneManager != null)
        {
            int managerPlayerId = onlineGameSceneManager.GetOnlinePlayerIdByActorNumber(actorNumber);

            if (managerPlayerId >= 0)
            {
                return managerPlayerId;
            }
        }

        foreach (Player photonPlayer in PhotonNetwork.PlayerList)
        {
            if (photonPlayer != null && photonPlayer.ActorNumber == actorNumber)
            {
                return GetPhotonPlayerIntProperty(photonPlayer, PhotonLobbyPropertyKeys.PlayerId, -1);
            }
        }

        return -1;
#endif
    }

    private int GetPhotonPlayerIntProperty(Player photonPlayer, string key, int fallbackValue)
    {
        if (photonPlayer == null ||
            photonPlayer.CustomProperties == null ||
            !photonPlayer.CustomProperties.ContainsKey(key))
        {
            return fallbackValue;
        }

        return ConvertToInt(photonPlayer.CustomProperties[key], fallbackValue);
    }

    private int ConvertToInt(object value, int fallbackValue)
    {
        if (value == null)
        {
            return fallbackValue;
        }

        if (value is int intValue)
        {
            return intValue;
        }

        if (value is short shortValue)
        {
            return shortValue;
        }

        if (value is byte byteValue)
        {
            return byteValue;
        }

        if (value is string stringValue && int.TryParse(stringValue, out int parsedValue))
        {
            return parsedValue;
        }

        return fallbackValue;
    }

    private OnlineCardRequestData DeserializeRequest(string json)
    {
        return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<OnlineCardRequestData>(json);
    }

    private OnlineCardApplyData DeserializeApply(string json)
    {
        return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<OnlineCardApplyData>(json);
    }

    private OnlinePrivateHandStateData DeserializePrivateState(string json)
    {
        return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<OnlinePrivateHandStateData>(json);
    }

    private Dictionary<int, int> BuildHandCountMap(OnlineCardPublicSnapshot snapshot)
    {
        Dictionary<int, int> counts = new Dictionary<int, int>();

        if (snapshot == null || snapshot.playerHandCounts == null)
        {
            return counts;
        }

        for (int i = 0; i < snapshot.playerHandCounts.Count; i++)
        {
            OnlinePlayerHandCountState handState = snapshot.playerHandCounts[i];

            if (handState == null)
            {
                continue;
            }

            counts[handState.playerId] = Mathf.Max(0, handState.handCount);
        }

        return counts;
    }

    private string FormatSnapshotCounts(OnlineCardPublicSnapshot snapshot)
    {
        if (snapshot == null || snapshot.playerHandCounts == null || snapshot.playerHandCounts.Count == 0)
        {
            return "(empty)";
        }

        List<string> parts = new List<string>();

        for (int i = 0; i < snapshot.playerHandCounts.Count; i++)
        {
            OnlinePlayerHandCountState handState = snapshot.playerHandCounts[i];

            if (handState == null)
            {
                continue;
            }

            parts.Add("P" + handState.playerId + "=" + Mathf.Max(0, handState.handCount));
        }

        return string.Join(", ", parts);
    }

    private void LogAuthoritativeHandCountTransition(
        OnlineCardRequestData request,
        OnlineCardApplyData applyData,
        OnlineCardPublicSnapshot beforeSnapshot,
        OnlineCardPublicSnapshot afterSnapshot)
    {
        string actionLabel = request != null ? request.actionType.ToString() : "Unknown";
        int actorPlayerId = request != null ? request.actorPlayerId : (applyData != null ? applyData.actorPlayerId : -1);
        int privateOwnerHandCount = 0;

        if (actorPlayerId >= 0)
        {
            privateOwnerHandCount = GetOrCreateAuthoritativeHand(actorPlayerId).Count;
        }

        Debug.Log(
            "Online card hand-count transition" +
            " actorPlayerId=" + actorPlayerId +
            " actionType=" + actionLabel +
            " publicBefore=[" + FormatSnapshotCounts(beforeSnapshot) + "]" +
            " publicAfter=[" + FormatSnapshotCounts(afterSnapshot) + "]" +
            " privateOwnerHandCount=" + privateOwnerHandCount
        );
    }

    private bool IsKnownEmptyPlayerSlot(PlayerResource player)
    {
        if (player == null)
        {
            return true;
        }

        return string.Equals(player.displayName, "Empty", System.StringComparison.OrdinalIgnoreCase);
    }

    private void RegisterPhotonCallbacksIfNeeded()
    {
#if PHOTON_UNITY_NETWORKING
        if (photonCallbacksRegistered)
        {
            return;
        }

        PhotonNetwork.AddCallbackTarget(this);
        photonCallbacksRegistered = true;
#endif
    }

    private void UnregisterPhotonCallbacksIfNeeded()
    {
#if PHOTON_UNITY_NETWORKING
        if (!photonCallbacksRegistered)
        {
            return;
        }

        PhotonNetwork.RemoveCallbackTarget(this);
        photonCallbacksRegistered = false;
#endif
    }

    private bool CanRunOnlineGameSceneCardSync()
    {
#if PHOTON_UNITY_NETWORKING
        return PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext() &&
            onlineGameSceneManager != null &&
            onlineGameSceneManager.IsOnlineModeActive &&
            PhotonNetwork.InRoom &&
            !PhotonNetwork.OfflineMode &&
            PhotonNetwork.CurrentRoom != null;
#else
        return false;
#endif
    }
}
