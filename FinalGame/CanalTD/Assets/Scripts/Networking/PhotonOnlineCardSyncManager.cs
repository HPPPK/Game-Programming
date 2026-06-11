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
    private readonly Dictionary<string, GateFrameAnimation> gateTargetsById = new Dictionary<string, GateFrameAnimation>();
    private readonly Dictionary<string, CannonTower> towerTargetsById = new Dictionary<string, CannonTower>();
    private readonly Dictionary<string, PathNode> pathNodesById = new Dictionary<string, PathNode>();
    private bool initializedForOnlineMatch;
    private bool photonCallbacksRegistered;

    public bool IsInitializedForOnlineMatch
    {
        get { return initializedForOnlineMatch; }
    }

    /// <summary>
    /// Finds and stores Photon online card sync manager references before scene gameplay begins.
    /// </summary>
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

    /// <summary>
    /// Unsubscribes Photon online card sync manager from events so disabled objects stop receiving callbacks.
    /// </summary>
    private void OnDisable()
    {
        UnregisterPhotonCallbacksIfNeeded();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Prepares this manager for a Photon match and loads or sends the starting synchronized state.
    /// </summary>
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

    /// <summary>
    /// Reads updated Photon room properties and reapplies the matching local synced state.
    /// </summary>
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

    /// <summary>
    /// Asks the Master Client to validate and synchronize this player's card draw.
    /// </summary>
    public bool RequestDrawCard()
    {
        /// <summary>
        /// Handles request action for Photon online card sync manager.
        /// </summary>
        return RequestAction(OnlineCardActionType.Draw, string.Empty, string.Empty, string.Empty, -1);
    }

    /// <summary>
    /// Asks the Master Client to validate and synchronize discarding the selected card.
    /// </summary>
    public bool RequestDiscardCard(string cardId, string cardName)
    {
        /// <summary>
        /// Handles request action for Photon online card sync manager.
        /// </summary>
        return RequestAction(OnlineCardActionType.Discard, cardId, cardName, string.Empty, -1);
    }

    /// <summary>
    /// Sends the selected card and target to the Master Client for validation and broadcast.
    /// </summary>
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
        else if (IsLockGateCardId(normalizedCardId))
        {
            Debug.Log(
                "RequestLockGate" +
                " actorPlayerId=" + (onlineGameSceneManager != null ? onlineGameSceneManager.LocalPlayerId : -1) +
                " targetGateId=" + targetId +
                " cardId=" + normalizedCardId +
                " cardName=" + cardName
            );
        }
        else if (IsOpenGateCardId(normalizedCardId))
        {
            Debug.Log(
                "RequestOpenGate" +
                " actorPlayerId=" + (onlineGameSceneManager != null ? onlineGameSceneManager.LocalPlayerId : -1) +
                " targetGateId=" + targetId +
                " cardId=" + normalizedCardId +
                " cardName=" + cardName
            );
        }
        else if (IsPowerBoostCardId(normalizedCardId))
        {
            Debug.Log(
                "RequestPowerBoost" +
                " actorPlayerId=" + (onlineGameSceneManager != null ? onlineGameSceneManager.LocalPlayerId : -1) +
                " targetTowerId=" + targetId +
                " accepted=pending reason=(request)" +
                " cardId=" + normalizedCardId +
                " cardName=" + cardName
            );
        }
        else if (IsShockTrapCardId(normalizedCardId))
        {
            Debug.Log(
                "RequestShockTrap" +
                " actorPlayerId=" + (onlineGameSceneManager != null ? onlineGameSceneManager.LocalPlayerId : -1) +
                " targetNodeId=" + targetId +
                " accepted=pending reason=(request)" +
                " cardId=" + normalizedCardId +
                " cardName=" + cardName
            );
        }

        /// <summary>
        /// Handles request action for Photon online card sync manager.
        /// </summary>
        return RequestAction(OnlineCardActionType.Play, cardId, cardName, targetId, targetPlayerId);
    }

    /// <summary>
    /// Responds to on event and updates the affected gameplay or UI systems.
    /// </summary>
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
    /// <summary>
    /// Master Client validates the incoming request, changes authoritative state, and sends the result.
    /// </summary>
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

    /// <summary>
    /// Checks whether request has valid state, permissions, targets, and resources before applying it.
    /// </summary>
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
            targetGateId = request.targetId,
            targetTowerId = request.targetId,
            targetNodeId = request.targetId,
            targetPlayerId = request.targetPlayerId,
            timestamp = request.timestamp,
            accepted = false
        };

        int resolvedSenderPlayerId = ResolvePlayerIdForActorNumber(senderActorNumber);

        if (!PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext())
        {
            /// <summary>
            /// Handles reject validation for Photon online card sync manager.
            /// </summary>
            return RejectValidation(request, result, "Online game inactive.");
        }

        if (!initializedForOnlineMatch)
        {
            /// <summary>
            /// Handles reject validation for Photon online card sync manager.
            /// </summary>
            return RejectValidation(request, result, "Card sync not ready.");
        }

        if (resolvedSenderPlayerId != request.actorPlayerId)
        {
            /// <summary>
            /// Handles reject validation for Photon online card sync manager.
            /// </summary>
            return RejectValidation(request, result, "Sender mismatch.");
        }

        if (onlineGameSceneManager == null ||
            onlineGameSceneManager.CurrentTurnPlayerId != request.actorPlayerId ||
            request.currentTurnPlayerId != request.actorPlayerId)
        {
            /// <summary>
            /// Handles reject validation for Photon online card sync manager.
            /// </summary>
            return RejectValidation(request, result, "Not current turn player.");
        }

        List<string> authoritativeHand = GetOrCreateAuthoritativeHand(request.actorPlayerId);
        int handLimit = cardDrawManager != null ? cardDrawManager.GetMaxHandSizeForPlayer(request.actorPlayerId) : 5;

        switch (request.actionType)
        {
            case OnlineCardActionType.Draw:
                if (turnManager != null && !turnManager.CanDrawCard())
                {
                    /// <summary>
                    /// Handles reject validation for Photon online card sync manager.
                    /// </summary>
                    return RejectValidation(request, result, "Draw already used.");
                }

                if (deckCardIds.Count <= 0)
                {
                    /// <summary>
                    /// Handles reject validation for Photon online card sync manager.
                    /// </summary>
                    return RejectValidation(request, result, "Deck is empty.");
                }

                if (authoritativeHand.Count >= handLimit)
                {
                    /// <summary>
                    /// Handles reject validation for Photon online card sync manager.
                    /// </summary>
                    return RejectValidation(request, result, "Hand limit reached.");
                }
                break;

            case OnlineCardActionType.Discard:
                if (turnManager != null && !turnManager.CanDiscardCard())
                {
                    /// <summary>
                    /// Handles reject validation for Photon online card sync manager.
                    /// </summary>
                    return RejectValidation(request, result, "Discard already used.");
                }

                if (!HandContainsCardId(authoritativeHand, request.cardId))
                {
                    /// <summary>
                    /// Handles reject validation for Photon online card sync manager.
                    /// </summary>
                    return RejectValidation(request, result, "Card not in hand.");
                }
                break;

            case OnlineCardActionType.Play:
                if (turnManager != null && !turnManager.CanPlayCard())
                {
                    /// <summary>
                    /// Handles reject validation for Photon online card sync manager.
                    /// </summary>
                    return RejectValidation(request, result, "Play unavailable.");
                }

                if (turnManager != null && IsGateControlCardId(request.cardId) && !turnManager.CanChangeGate())
                {
                    /// <summary>
                    /// Handles reject validation for Photon online card sync manager.
                    /// </summary>
                    return RejectValidation(request, result, "Gate change already used.");
                }

                if (!HandContainsCardId(authoritativeHand, request.cardId))
                {
                    /// <summary>
                    /// Handles reject validation for Photon online card sync manager.
                    /// </summary>
                    return RejectValidation(request, result, "Card not in hand.");
                }

                if (!HasValidTargetMetadata(request.cardId, request.targetId, request.targetPlayerId, request.actorPlayerId))
                {
                    /// <summary>
                    /// Handles reject validation for Photon online card sync manager.
                    /// </summary>
                    return RejectValidation(request, result, "Invalid target metadata.");
                }

                if (IsPlayerInteractionCardId(request.cardId))
                {
                    string cardEffectRejectReason = ValidatePlayerInteractionCardEffect(request);

                    if (!string.IsNullOrWhiteSpace(cardEffectRejectReason))
                    {
                        /// <summary>
                        /// Handles reject validation for Photon online card sync manager.
                        /// </summary>
                        return RejectValidation(request, result, cardEffectRejectReason);
                    }
                }
                else if (IsLandControlCardId(request.cardId))
                {
                    string tileRejectReason = ValidateLandControlCardEffect(request, result);

                    if (!string.IsNullOrWhiteSpace(tileRejectReason))
                    {
                        /// <summary>
                        /// Handles reject validation for Photon online card sync manager.
                        /// </summary>
                        return RejectValidation(request, result, tileRejectReason);
                    }
                }
                else if (IsGateControlCardId(request.cardId))
                {
                    string gateRejectReason = ValidateGateControlCardEffect(request, result);

                    if (!string.IsNullOrWhiteSpace(gateRejectReason))
                    {
                        /// <summary>
                        /// Handles reject validation for Photon online card sync manager.
                        /// </summary>
                        return RejectValidation(request, result, gateRejectReason);
                    }
                }
                else if (IsPowerBoostCardId(request.cardId))
                {
                    string powerBoostRejectReason = ValidatePowerBoostCardEffect(request, result);

                    if (!string.IsNullOrWhiteSpace(powerBoostRejectReason))
                    {
                        /// <summary>
                        /// Handles reject validation for Photon online card sync manager.
                        /// </summary>
                        return RejectValidation(request, result, powerBoostRejectReason);
                    }
                }
                else if (IsShockTrapCardId(request.cardId))
                {
                    string shockTrapRejectReason = ValidateShockTrapCardEffect(request, result);

                    if (!string.IsNullOrWhiteSpace(shockTrapRejectReason))
                    {
                        /// <summary>
                        /// Handles reject validation for Photon online card sync manager.
                        /// </summary>
                        return RejectValidation(request, result, shockTrapRejectReason);
                    }
                }
                break;

            default:
                /// <summary>
                /// Handles reject validation for Photon online card sync manager.
                /// </summary>
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
        else if (IsLockGateCardId(request.cardId))
        {
            LogGateControlValidation("ValidateLockGate", request, result, "(none)");
        }
        else if (IsOpenGateCardId(request.cardId))
        {
            LogGateControlValidation("ValidateOpenGate", request, result, "(none)");
        }
        else if (IsPowerBoostCardId(request.cardId))
        {
            LogPowerBoostValidation("ValidatePowerBoost", request, result, "(none)");
        }
        else if (IsShockTrapCardId(request.cardId))
        {
            LogShockTrapValidation("ValidateShockTrap", request, result, "(none)");
        }

        return result;
    }

    /// <summary>
    /// Applies synchronized authoritative mutation to this client so it matches the authoritative state.
    /// </summary>
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
            else if (IsGateControlCardId(normalizedCardId))
            {
                ApplyGateControlEffect(request, authoritativeHand, applyData);
            }
            else if (IsPowerBoostCardId(normalizedCardId))
            {
                ApplyPowerBoostEffect(request, authoritativeHand, applyData);
            }
            else if (IsShockTrapCardId(normalizedCardId))
            {
                ApplyShockTrapEffect(request, authoritativeHand, applyData);
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

    /// <summary>
    /// Coordinates broadcast apply to all for Photon synchronization and local scene state.
    /// </summary>
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

    /// <summary>
    /// Sends a Photon request for private hand state from master so the Master Client can validate it.
    /// </summary>
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

    /// <summary>
    /// Processes private hand state request and routes the result to the correct state-changing logic.
    /// </summary>
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

    /// <summary>
    /// Extracts requested private hand player ID from event data, JSON, or Photon payloads.
    /// </summary>
    private int ExtractRequestedPrivateHandPlayerId(object requestData)
    {
        if (requestData is object[] requestArray && requestArray.Length > 0)
        {
            /// <summary>
            /// Handles convert to int for Photon online card sync manager.
            /// </summary>
            return ConvertToInt(requestArray[0], -1);
        }

        /// <summary>
        /// Handles convert to int for Photon online card sync manager.
        /// </summary>
        return ConvertToInt(requestData, -1);
    }

    /// <summary>
    /// Sends apply to requester to the correct Photon receiver or player owner.
    /// </summary>
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

    /// <summary>
    /// Sends private hand state to owner to the correct Photon receiver or player owner.
    /// </summary>
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

    /// <summary>
    /// Coordinates bootstrap authoritative state as master for Photon synchronization and local scene state.
    /// </summary>
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

    /// <summary>
    /// Sends private hand state to all active players to the correct Photon receiver or player owner.
    /// </summary>
    private void SendPrivateHandStateToAllActivePlayers()
    {
        foreach (PlayerResource player in GetActivePlayers())
        {
            SendPrivateHandStateToOwner(player.playerId);
        }
    }

    /// <summary>
    /// Applies synchronized snapshot from room properties to this client so it matches the authoritative state.
    /// </summary>
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

    /// <summary>
    /// Updates snapshot property so the display or cached state matches current gameplay data.
    /// </summary>
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

    /// <summary>
    /// Builds public snapshot for Photon messages, room properties, or debug logs.
    /// </summary>
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

    /// <summary>
    /// Applies an accepted online action to local gameplay state and visible UI feedback.
    /// </summary>
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
        ApplySyncedGateState(applyData);
        ApplySyncedPowerBoostState(applyData);
        ApplySyncedShockTrapState(applyData);

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

    /// <summary>
    /// Applies synchronized private hand state to this client so it matches the authoritative state.
    /// </summary>
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

    /// <summary>
    /// Applies synchronized synced tile state to this client so it matches the authoritative state.
    /// </summary>
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

    /// <summary>
    /// Applies synchronized synced gate state to this client so it matches the authoritative state.
    /// </summary>
    private void ApplySyncedGateState(OnlineCardApplyData applyData)
    {
        if (applyData == null || !IsGateControlCardId(applyData.cardId))
        {
            return;
        }

        ApplyGateStateToScene(applyData);

        Debug.Log(
            (IsLockGateCardId(applyData.cardId) ? "ApplyLockGate" : "ApplyOpenGate") +
            " actorPlayerId=" + applyData.actorPlayerId +
            " targetGateId=" + applyData.targetGateId +
            " previousGateState=" + applyData.previousGateState +
            " newGateState=" + applyData.newGateState +
            " accepted=true rejectedReason=(none)"
        );
    }

    /// <summary>
    /// Applies synchronized synced power boost state to this client so it matches the authoritative state.
    /// </summary>
    private void ApplySyncedPowerBoostState(OnlineCardApplyData applyData)
    {
        if (applyData == null || !IsPowerBoostCardId(applyData.cardId))
        {
            return;
        }

        ApplyPowerBoostStateToScene(applyData);

        Debug.Log(
            "ApplyPowerBoost" +
            " actorPlayerId=" + applyData.actorPlayerId +
            " targetTowerId=" + applyData.targetTowerId +
            " accepted=true reason=(none)" +
            " boostState=" + applyData.boostState
        );
    }

    /// <summary>
    /// Applies synchronized synced shock trap state to this client so it matches the authoritative state.
    /// </summary>
    private void ApplySyncedShockTrapState(OnlineCardApplyData applyData)
    {
        if (applyData == null || !IsShockTrapCardId(applyData.cardId))
        {
            return;
        }

        ApplyShockTrapStateToScene(applyData);

        Debug.Log(
            "ApplyShockTrap" +
            " actorPlayerId=" + applyData.actorPlayerId +
            " targetNodeId=" + applyData.targetNodeId +
            " trapId=" + applyData.trapId +
            " accepted=true reason=(none)" +
            " trapState=" + applyData.trapState
        );
    }

    /// <summary>
    /// Applies synchronized tile state to scene to this client so it matches the authoritative state.
    /// </summary>
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

    /// <summary>
    /// Applies synchronized gate state to scene to this client so it matches the authoritative state.
    /// </summary>
    private void ApplyGateStateToScene(OnlineCardApplyData applyData)
    {
        GateFrameAnimation targetGate = ResolveGateTargetById(applyData.targetGateId);

        if (targetGate == null)
        {
            Debug.LogWarning("Online card gate apply failed. targetGateId=" + applyData.targetGateId);
            return;
        }

        bool actionSucceeded = true;

        if (IsLockGateCardId(applyData.cardId))
        {
            if (!targetGate.IsBlocking())
            {
                actionSucceeded = targetGate.LockGate();
            }
            else
            {
                PathGraphState.MarkDirty();
            }
        }
        else if (IsOpenGateCardId(applyData.cardId))
        {
            if (targetGate.IsBlocking())
            {
                actionSucceeded = targetGate.OpenGate();
            }
            else
            {
                PathGraphState.MarkDirty();
            }
        }

        if (!actionSucceeded)
        {
            Debug.LogWarning(
                "Online gate state apply was rejected by local gate state" +
                " targetGateId=" + applyData.targetGateId +
                " cardId=" + applyData.cardId +
                " expectedNewState=" + applyData.newGateState
            );
        }

        Debug.Log(
            "Applied online gate state" +
            " targetGateId=" + applyData.targetGateId +
            " actualOpen=" + !targetGate.IsBlocking() +
            " actualLocked=" + targetGate.IsLocked() +
            " expectedNewState=" + applyData.newGateState
        );
    }

    /// <summary>
    /// Applies synchronized power boost state to scene to this client so it matches the authoritative state.
    /// </summary>
    private void ApplyPowerBoostStateToScene(OnlineCardApplyData applyData)
    {
        CannonTower targetTower = ResolveTowerTargetById(applyData.targetTowerId);

        if (targetTower == null)
        {
            Debug.LogWarning("Online power boost apply failed. targetTowerId=" + applyData.targetTowerId);
            return;
        }

        TowerTargetingManager towerTargetingManager = TowerTargetingManagerInstance();

        if (towerTargetingManager == null)
        {
            Debug.LogWarning("Online power boost apply failed. TowerTargetingManager is missing.");
            return;
        }

        bool actionSucceeded = targetTower.boostPendingForNextWave ||
            targetTower.boostActive ||
            towerTargetingManager.ApplyPowerBoostFromOnline(applyData.ownerPlayerId, targetTower);

        if (!actionSucceeded)
        {
            Debug.LogWarning(
                "Online power boost apply was rejected by local tower state" +
                " targetTowerId=" + applyData.targetTowerId +
                " ownerPlayerId=" + applyData.ownerPlayerId
            );
        }
    }

    /// <summary>
    /// Applies synchronized shock trap state to scene to this client so it matches the authoritative state.
    /// </summary>
    private void ApplyShockTrapStateToScene(OnlineCardApplyData applyData)
    {
        PathNode targetNode = ResolvePathNodeById(applyData.targetNodeId);

        if (targetNode == null)
        {
            Debug.LogWarning("Online shock trap apply failed. targetNodeId=" + applyData.targetNodeId);
            return;
        }

        ShockTrapTargetingManager shockTrapTargetingManager = ShockTrapTargetingManagerInstance();

        if (shockTrapTargetingManager == null)
        {
            Debug.LogWarning("Online shock trap apply failed. ShockTrapTargetingManager is missing.");
            return;
        }

        bool actionSucceeded = shockTrapTargetingManager.ApplyShockTrapPlacementFromOnline(
            applyData.ownerPlayerId,
            targetNode
        );

        if (!actionSucceeded)
        {
            Debug.LogWarning(
                "Online shock trap apply was rejected by local trap state" +
                " targetNodeId=" + applyData.targetNodeId +
                " ownerPlayerId=" + applyData.ownerPlayerId
            );
        }
    }

    /// <summary>
    /// Updates build snapshot for tile state so the display or cached state matches current gameplay data.
    /// </summary>
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

    /// <summary>
    /// Consumes local turn state and records that the player has used that action.
    /// </summary>
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

            if (IsGateControlCardId(applyData.cardId))
            {
                turnManager.ApplyAuthoritativeGateChangeConsumed();
            }
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

    /// <summary>
    /// Shows action toast with the correct current context.
    /// </summary>
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

        if (IsLockGateCardId(normalizedCardId))
        {
            onlineGameSceneManager.ShowOnlineToast(
                isLocalActor ? "You locked a gate." : actorName + " locked a gate."
            );
            return;
        }

        if (IsOpenGateCardId(normalizedCardId))
        {
            onlineGameSceneManager.ShowOnlineToast(
                isLocalActor ? "You opened a gate." : actorName + " opened a gate."
            );
            return;
        }

        if (IsPowerBoostCardId(normalizedCardId))
        {
            onlineGameSceneManager.ShowOnlineToast(
                isLocalActor ? "You boosted a tower." : actorName + " boosted a tower."
            );
            return;
        }

        if (IsShockTrapCardId(normalizedCardId))
        {
            onlineGameSceneManager.ShowOnlineToast(
                isLocalActor ? "You placed a Shock Trap." : actorName + " placed a Shock Trap."
            );
            return;
        }

        string playedCardName = string.IsNullOrWhiteSpace(applyData.cardName) ? "a card" : applyData.cardName;
        onlineGameSceneManager.ShowOnlineToast(isLocalActor
            ? "You played " + playedCardName + "."
            : actorName + " played " + playedCardName + ".");
    }

    /// <summary>
    /// Coordinates auto assign references for Photon synchronization and local scene state.
    /// </summary>
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
        RebuildGateTargetRegistryIfNeeded();
        RebuildTowerTargetRegistryIfNeeded();
        RebuildPathNodeRegistryIfNeeded();
    }

    /// <summary>
    /// Looks up the target for tile target by ID and applies the resolved gameplay result.
    /// </summary>
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

    /// <summary>
    /// Rebuilds tile target registry if needed from current scene objects or synchronized data.
    /// </summary>
    private void RebuildTileTargetRegistryIfNeeded()
    {
        if (tileTargetsById.Count > 0)
        {
            return;
        }

        RebuildTileTargetRegistry();
    }

    /// <summary>
    /// Rebuilds tile target registry from current scene objects or synchronized data.
    /// </summary>
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

    /// <summary>
    /// Looks up the target for gate target by ID and applies the resolved gameplay result.
    /// </summary>
    private GateFrameAnimation ResolveGateTargetById(string targetGateId)
    {
        RebuildGateTargetRegistryIfNeeded();

        if (string.IsNullOrWhiteSpace(targetGateId))
        {
            return null;
        }

        gateTargetsById.TryGetValue(targetGateId, out GateFrameAnimation targetGate);
        return targetGate;
    }

    /// <summary>
    /// Rebuilds gate target registry if needed from current scene objects or synchronized data.
    /// </summary>
    private void RebuildGateTargetRegistryIfNeeded()
    {
        if (gateTargetsById.Count > 0)
        {
            return;
        }

        RebuildGateTargetRegistry();
    }

    /// <summary>
    /// Rebuilds gate target registry from current scene objects or synchronized data.
    /// </summary>
    private void RebuildGateTargetRegistry()
    {
        gateTargetsById.Clear();
        GateFrameAnimation[] gates = FindObjectsOfType<GateFrameAnimation>(true);

        foreach (GateFrameAnimation gate in gates)
        {
            if (gate == null || string.IsNullOrWhiteSpace(gate.name))
            {
                continue;
            }

            if (gateTargetsById.ContainsKey(gate.name))
            {
                Debug.LogWarning("Duplicate online gate target id found: " + gate.name);
                continue;
            }

            gateTargetsById.Add(gate.name, gate);
        }
    }

    /// <summary>
    /// Looks up the target for tower target by ID and applies the resolved gameplay result.
    /// </summary>
    private CannonTower ResolveTowerTargetById(string targetTowerId)
    {
        RebuildTowerTargetRegistryIfNeeded();

        if (string.IsNullOrWhiteSpace(targetTowerId))
        {
            return null;
        }

        towerTargetsById.TryGetValue(targetTowerId, out CannonTower targetTower);
        return targetTower;
    }

    /// <summary>
    /// Rebuilds tower target registry if needed from current scene objects or synchronized data.
    /// </summary>
    private void RebuildTowerTargetRegistryIfNeeded()
    {
        if (towerTargetsById.Count > 0)
        {
            return;
        }

        RebuildTowerTargetRegistry();
    }

    /// <summary>
    /// Rebuilds tower target registry from current scene objects or synchronized data.
    /// </summary>
    private void RebuildTowerTargetRegistry()
    {
        towerTargetsById.Clear();
        CannonTower[] towers = FindObjectsOfType<CannonTower>(true);

        foreach (CannonTower tower in towers)
        {
            RegisterTowerTargetId(tower != null ? tower.gameObject.name : string.Empty, tower);

            if (tower != null && tower.transform.parent != null)
            {
                RegisterTowerTargetId(tower.transform.parent.name + "_tower", tower);
            }
        }
    }

    /// <summary>
    /// Registers tower target ID so later callbacks, lookups, or sync messages can use it.
    /// </summary>
    private void RegisterTowerTargetId(string targetTowerId, CannonTower tower)
    {
        if (tower == null || string.IsNullOrWhiteSpace(targetTowerId))
        {
            return;
        }

        if (towerTargetsById.ContainsKey(targetTowerId))
        {
            Debug.LogWarning("Duplicate online tower target id found: " + targetTowerId);
            return;
        }

        towerTargetsById.Add(targetTowerId, tower);
    }

    /// <summary>
    /// Looks up the target for path node by ID and applies the resolved gameplay result.
    /// </summary>
    private PathNode ResolvePathNodeById(string targetNodeId)
    {
        RebuildPathNodeRegistryIfNeeded();

        if (string.IsNullOrWhiteSpace(targetNodeId))
        {
            return null;
        }

        pathNodesById.TryGetValue(targetNodeId, out PathNode targetNode);
        return targetNode;
    }

    /// <summary>
    /// Rebuilds path node registry if needed from current scene objects or synchronized data.
    /// </summary>
    private void RebuildPathNodeRegistryIfNeeded()
    {
        if (pathNodesById.Count > 0)
        {
            return;
        }

        RebuildPathNodeRegistry();
    }

    /// <summary>
    /// Rebuilds path node registry from current scene objects or synchronized data.
    /// </summary>
    private void RebuildPathNodeRegistry()
    {
        pathNodesById.Clear();
        PathNode[] pathNodes = FindObjectsOfType<PathNode>(true);

        foreach (PathNode node in pathNodes)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.name))
            {
                continue;
            }

            if (pathNodesById.ContainsKey(node.name))
            {
                Debug.LogWarning("Duplicate online path node target id found: " + node.name);
                continue;
            }

            pathNodesById.Add(node.name, node);
        }
    }

    /// <summary>
    /// Returns active players from Photon, room data, or the online player cache.
    /// </summary>
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

    /// <summary>
    /// Returns or create authoritative hand from Photon, room data, or the online player cache.
    /// </summary>
    private List<string> GetOrCreateAuthoritativeHand(int playerId)
    {
        if (!playerHandCardIds.TryGetValue(playerId, out List<string> hand))
        {
            hand = new List<string>();
            playerHandCardIds[playerId] = hand;
        }

        return hand;
    }

    /// <summary>
    /// Coordinates hand contains card ID for Photon synchronization and local scene state.
    /// </summary>
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

    /// <summary>
    /// Removes card ID from hand from the scene, list, UI, hand, deck, or gameplay state.
    /// </summary>
    private bool RemoveCardIdFromHand(List<string> hand, string cardId)
    {
        string removedCardId;
        /// <summary>
        /// Handles remove card ID from hand for Photon online card sync manager.
        /// </summary>
        return RemoveCardIdFromHand(hand, cardId, out removedCardId);
    }

    /// <summary>
    /// Removes card ID from hand from the scene, list, UI, hand, deck, or gameplay state.
    /// </summary>
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

    /// <summary>
    /// Coordinates card IDs match for Photon synchronization and local scene state.
    /// </summary>
    private bool CardIdsMatch(string leftCardId, string rightCardId)
    {
        /// <summary>
        /// Handles canonical card ID for Photon online card sync manager.
        /// </summary>
        return CanonicalCardId(leftCardId) == CanonicalCardId(rightCardId);
    }

    /// <summary>
    /// Checks whether canonical card ID is allowed before enabling that action.
    /// </summary>
    private string CanonicalCardId(string cardId)
    {
        return CardDrawManager.NormalizeCardId(cardId).Replace(" ", string.Empty);
    }

    /// <summary>
    /// Builds private hand state for Photon messages, room properties, or debug logs.
    /// </summary>
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

    /// <summary>
    /// Shuffles deck card IDs so future selections happen in random order.
    /// </summary>
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

    /// <summary>
    /// Checks whether valid target metadata is present before the code depends on it.
    /// </summary>
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

    /// <summary>
    /// Coordinates reject validation for Photon synchronization and local scene state.
    /// </summary>
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
            else if (IsLockGateCardId(request.cardId))
            {
                LogGateControlValidation("ValidateLockGate", request, result, rejectReason);
            }
            else if (IsOpenGateCardId(request.cardId))
            {
                LogGateControlValidation("ValidateOpenGate", request, result, rejectReason);
            }
            else if (IsPowerBoostCardId(request.cardId))
            {
                LogPowerBoostValidation("ValidatePowerBoost", request, result, rejectReason);
            }
            else if (IsShockTrapCardId(request.cardId))
            {
                LogShockTrapValidation("ValidateShockTrap", request, result, rejectReason);
            }
        }

        return result;
    }

    /// <summary>
    /// Checks whether player interaction card effect has valid state, permissions, targets, and resources before applying it.
    /// </summary>
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

    /// <summary>
    /// Checks the current state to decide whether active target player is true.
    /// </summary>
    private bool IsActiveTargetPlayer(int playerId)
    {
        if (playerManager == null || playerId < 0)
        {
            return false;
        }

        PlayerResource targetPlayer = playerManager.GetPlayerResource(playerId);
        return targetPlayer != null && !targetPlayer.isEliminated && !IsKnownEmptyPlayerSlot(targetPlayer);
    }

    /// <summary>
    /// Checks the current state to decide whether player interaction card ID is true.
    /// </summary>
    private bool IsPlayerInteractionCardId(string cardId)
    {
        /// <summary>
        /// Handles is steal card ID for Photon online card sync manager.
        /// </summary>
        return IsStealCardId(cardId) || IsTradeHandsCardId(cardId) || IsDisruptCardId(cardId);
    }

    /// <summary>
    /// Checks the current state to decide whether land control card ID is true.
    /// </summary>
    private bool IsLandControlCardId(string cardId)
    {
        /// <summary>
        /// Handles is take over card ID for Photon online card sync manager.
        /// </summary>
        return IsTakeOverCardId(cardId) || IsFreezeClaimCardId(cardId);
    }

    /// <summary>
    /// Checks the current state to decide whether gate control card ID is true.
    /// </summary>
    private bool IsGateControlCardId(string cardId)
    {
        /// <summary>
        /// Handles is lock gate card ID for Photon online card sync manager.
        /// </summary>
        return IsLockGateCardId(cardId) || IsOpenGateCardId(cardId);
    }

    /// <summary>
    /// Checks the current state to decide whether power boost card ID is true.
    /// </summary>
    private bool IsPowerBoostCardId(string cardId)
    {
        string normalizedCardId = CardDrawManager.NormalizeCardId(cardId);
        return normalizedCardId == "powerboost" || normalizedCardId == "power boost";
    }

    /// <summary>
    /// Checks the current state to decide whether shock trap card ID is true.
    /// </summary>
    private bool IsShockTrapCardId(string cardId)
    {
        string normalizedCardId = CardDrawManager.NormalizeCardId(cardId);
        return normalizedCardId == "shocktrap" || normalizedCardId == "shock trap";
    }

    /// <summary>
    /// Checks the current state to decide whether steal card ID is true.
    /// </summary>
    private bool IsStealCardId(string cardId)
    {
        string normalizedCardId = CardDrawManager.NormalizeCardId(cardId);
        return normalizedCardId == "stealcard" || normalizedCardId == "steal card";
    }

    /// <summary>
    /// Checks the current state to decide whether trade hands card ID is true.
    /// </summary>
    private bool IsTradeHandsCardId(string cardId)
    {
        string normalizedCardId = CardDrawManager.NormalizeCardId(cardId);
        return normalizedCardId == "tradehands" || normalizedCardId == "trade hands";
    }

    /// <summary>
    /// Checks the current state to decide whether disrupt card ID is true.
    /// </summary>
    private bool IsDisruptCardId(string cardId)
    {
        return CardDrawManager.NormalizeCardId(cardId) == "disrupt";
    }

    /// <summary>
    /// Checks the current state to decide whether take over card ID is true.
    /// </summary>
    private bool IsTakeOverCardId(string cardId)
    {
        string normalizedCardId = CardDrawManager.NormalizeCardId(cardId);
        return normalizedCardId == "takeover" || normalizedCardId == "take over";
    }

    /// <summary>
    /// Checks the current state to decide whether freeze claim card ID is true.
    /// </summary>
    private bool IsFreezeClaimCardId(string cardId)
    {
        string normalizedCardId = CardDrawManager.NormalizeCardId(cardId);
        return normalizedCardId == "freezeclaim" || normalizedCardId == "freeze claim";
    }

    /// <summary>
    /// Checks the current state to decide whether lock gate card ID is true.
    /// </summary>
    private bool IsLockGateCardId(string cardId)
    {
        string normalizedCardId = CardDrawManager.NormalizeCardId(cardId);
        return normalizedCardId == "lockgate" || normalizedCardId == "lock gate";
    }

    /// <summary>
    /// Checks the current state to decide whether open gate card ID is true.
    /// </summary>
    private bool IsOpenGateCardId(string cardId)
    {
        string normalizedCardId = CardDrawManager.NormalizeCardId(cardId);
        return normalizedCardId == "opengate" ||
            normalizedCardId == "open gate" ||
            normalizedCardId == "redirectflow" ||
            normalizedCardId == "redirect flow";
    }

    /// <summary>
    /// Checks whether land control card effect has valid state, permissions, targets, and resources before applying it.
    /// </summary>
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

    /// <summary>
    /// Checks whether gate control card effect has valid state, permissions, targets, and resources before applying it.
    /// </summary>
    private string ValidateGateControlCardEffect(OnlineCardRequestData request, OnlineCardApplyData result)
    {
        if (request == null)
        {
            return "Invalid request.";
        }

        GateFrameAnimation targetGate = ResolveGateTargetById(request.targetId);

        if (targetGate == null)
        {
            return "Target gate is missing.";
        }

        result.targetGateId = request.targetId;
        result.previousGateOpen = !targetGate.IsBlocking();
        result.newGateOpen = !targetGate.IsBlocking();
        result.previousGateLocked = targetGate.IsLocked();
        result.newGateLocked = targetGate.IsLocked();
        result.previousGateState = DescribeGateState(targetGate);
        result.newGateState = result.previousGateState;

        GateActionType actionType = IsLockGateCardId(request.cardId)
            ? GateActionType.LockGate
            : GateActionType.OpenGate;

        GateTargetingManager gateTargetingManager = GateTargetingManager.Instance != null
            ? GateTargetingManager.Instance
            : FindObjectOfType<GateTargetingManager>();

        if (gateTargetingManager == null)
        {
            return "Gate targeting manager is missing.";
        }

        List<GateFrameAnimation> validGates = gateTargetingManager.GetValidGatesForPlayer(
            actionType,
            request.actorPlayerId
        );

        bool targetIsValid = false;

        if (validGates != null)
        {
            for (int i = 0; i < validGates.Count; i++)
            {
                if (validGates[i] == targetGate)
                {
                    targetIsValid = true;
                    break;
                }
            }
        }

        if (!targetIsValid)
        {
            /// <summary>
            /// Handles is lock gate card ID for Photon online card sync manager.
            /// </summary>
            return IsLockGateCardId(request.cardId)
                ? "This gate cannot be locked."
                : "This gate cannot be opened.";
        }

        if (IsLockGateCardId(request.cardId))
        {
            result.newGateOpen = false;
            result.newGateLocked = targetGate.IsLocked();
            result.newGateState = "Closed";
            return string.Empty;
        }

        result.newGateOpen = true;
        result.newGateLocked = false;
        result.newGateState = "Open";
        return string.Empty;
    }

    /// <summary>
    /// Checks whether power boost card effect has valid state, permissions, targets, and resources before applying it.
    /// </summary>
    private string ValidatePowerBoostCardEffect(OnlineCardRequestData request, OnlineCardApplyData result)
    {
        if (request == null)
        {
            return "Invalid request.";
        }

        CannonTower targetTower = ResolveTowerTargetById(request.targetId);

        if (targetTower == null)
        {
            return "Target tower is missing.";
        }

        TowerTargetingManager towerTargetingManager = TowerTargetingManagerInstance();

        if (towerTargetingManager == null)
        {
            return "Tower targeting manager is missing.";
        }

        result.targetTowerId = request.targetId;
        result.ownerPlayerId = request.actorPlayerId;
        result.boostPendingForNextWave = targetTower.boostPendingForNextWave;
        result.boostActive = targetTower.boostActive;
        result.boostState = DescribeBoostState(targetTower);

        if (!towerTargetingManager.CanPlayerBoostTower(request.actorPlayerId, targetTower))
        {
            return "Target tower cannot be boosted.";
        }

        result.boostPendingForNextWave = true;
        result.boostActive = false;
        result.boostState = "PendingForNextWave";
        result.boostStartTurn = -1;
        result.boostEndTurn = -1;
        return string.Empty;
    }

    /// <summary>
    /// Checks whether shock trap card effect has valid state, permissions, targets, and resources before applying it.
    /// </summary>
    private string ValidateShockTrapCardEffect(OnlineCardRequestData request, OnlineCardApplyData result)
    {
        if (request == null)
        {
            return "Invalid request.";
        }

        PathNode targetNode = ResolvePathNodeById(request.targetId);

        if (targetNode == null)
        {
            return "Target node is missing.";
        }

        ShockTrapTargetingManager shockTrapTargetingManager = ShockTrapTargetingManagerInstance();

        if (shockTrapTargetingManager == null)
        {
            return "Shock Trap targeting manager is missing.";
        }

        result.targetNodeId = request.targetId;
        result.ownerPlayerId = request.actorPlayerId;
        result.trapId = BuildTrapId(request.actorPlayerId, request.targetId);
        result.trapState = "PendingPlacement";

        if (!shockTrapTargetingManager.CanPlaceShockTrapAtNode(targetNode))
        {
            return "Target node cannot receive a Shock Trap.";
        }

        result.trapState = "Placed";
        return string.Empty;
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

    private void ApplyGateControlEffect(
        OnlineCardRequestData request,
        List<string> actorHand,
        OnlineCardApplyData applyData)
    {
        GateFrameAnimation targetGate = ResolveGateTargetById(request.targetId);

        if (targetGate == null)
        {
            applyData.accepted = false;
            applyData.rejectReason = "Target gate is missing.";
            return;
        }

        RemoveCardIdFromHand(actorHand, request.cardId);

        applyData.targetGateId = request.targetId;
        applyData.previousGateOpen = !targetGate.IsBlocking();
        applyData.previousGateLocked = targetGate.IsLocked();
        applyData.previousGateState = DescribeGateState(targetGate);
        applyData.newGateOpen = IsOpenGateCardId(request.cardId);
        applyData.newGateLocked = IsOpenGateCardId(request.cardId) ? false : targetGate.IsLocked();
        applyData.newGateState = applyData.newGateOpen ? "Open" : "Closed";
        applyData.affectedPlayerHandCounts = BuildAffectedHandCounts(request.actorPlayerId);

        ApplyGateStateToScene(applyData);

        Debug.Log(
            (IsLockGateCardId(request.cardId) ? "ApplyLockGate" : "ApplyOpenGate") +
            " actorPlayerId=" + request.actorPlayerId +
            " targetGateId=" + request.targetId +
            " previousGateState=" + applyData.previousGateState +
            " newGateState=" + applyData.newGateState +
            " accepted=true rejectedReason=(none)"
        );
    }

    private void ApplyPowerBoostEffect(
        OnlineCardRequestData request,
        List<string> actorHand,
        OnlineCardApplyData applyData)
    {
        CannonTower targetTower = ResolveTowerTargetById(request.targetId);

        if (targetTower == null)
        {
            applyData.accepted = false;
            applyData.rejectReason = "Target tower is missing.";
            return;
        }

        RemoveCardIdFromHand(actorHand, request.cardId);

        applyData.targetTowerId = request.targetId;
        applyData.ownerPlayerId = request.actorPlayerId;
        applyData.boostStartTurn = -1;
        applyData.boostEndTurn = -1;
        applyData.boostPendingForNextWave = true;
        applyData.boostActive = false;
        applyData.boostState = "PendingForNextWave";
        applyData.affectedPlayerHandCounts = BuildAffectedHandCounts(request.actorPlayerId);

        ApplyPowerBoostStateToScene(applyData);

        Debug.Log(
            "ApplyPowerBoost" +
            " actorPlayerId=" + request.actorPlayerId +
            " targetTowerId=" + request.targetId +
            " accepted=true reason=(none)" +
            " boostState=" + applyData.boostState
        );
    }

    private void ApplyShockTrapEffect(
        OnlineCardRequestData request,
        List<string> actorHand,
        OnlineCardApplyData applyData)
    {
        PathNode targetNode = ResolvePathNodeById(request.targetId);

        if (targetNode == null)
        {
            applyData.accepted = false;
            applyData.rejectReason = "Target node is missing.";
            return;
        }

        RemoveCardIdFromHand(actorHand, request.cardId);

        applyData.targetNodeId = request.targetId;
        applyData.ownerPlayerId = request.actorPlayerId;
        applyData.trapId = BuildTrapId(request.actorPlayerId, request.targetId);
        applyData.trapState = "Placed";
        applyData.affectedPlayerHandCounts = BuildAffectedHandCounts(request.actorPlayerId);

        ApplyShockTrapStateToScene(applyData);

        Debug.Log(
            "ApplyShockTrap" +
            " actorPlayerId=" + request.actorPlayerId +
            " targetNodeId=" + request.targetId +
            " trapId=" + applyData.trapId +
            " accepted=true reason=(none)" +
            " trapState=" + applyData.trapState
        );
    }

    /// <summary>
    /// Builds affected hand counts for Photon messages, room properties, or debug logs.
    /// </summary>
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

    /// <summary>
    /// Adds affected hand count state to the relevant list, UI, hand, deck, or gameplay state.
    /// </summary>
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

    /// <summary>
    /// Sends private hand states for accepted action to the correct Photon receiver or player owner.
    /// </summary>
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

    /// <summary>
    /// Writes card effect validation details to the Unity Console for debugging and validation.
    /// </summary>
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

    /// <summary>
    /// Writes land control validation details to the Unity Console for debugging and validation.
    /// </summary>
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

    /// <summary>
    /// Writes gate control validation details to the Unity Console for debugging and validation.
    /// </summary>
    private void LogGateControlValidation(string label, OnlineCardRequestData request, OnlineCardApplyData applyData, string reason)
    {
        if (request == null || applyData == null)
        {
            return;
        }

        Debug.Log(
            label +
            " actorPlayerId=" + request.actorPlayerId +
            " targetGateId=" + request.targetId +
            " previousGateState=" + applyData.previousGateState +
            " newGateState=" + applyData.newGateState +
            " accepted=" + applyData.accepted +
            " rejectedReason=" + (string.IsNullOrWhiteSpace(reason) ? "(none)" : reason)
        );
    }

    /// <summary>
    /// Writes power boost validation details to the Unity Console for debugging and validation.
    /// </summary>
    private void LogPowerBoostValidation(string label, OnlineCardRequestData request, OnlineCardApplyData applyData, string reason)
    {
        if (request == null || applyData == null)
        {
            return;
        }

        Debug.Log(
            label +
            " actorPlayerId=" + request.actorPlayerId +
            " targetTowerId=" + request.targetId +
            " accepted=" + applyData.accepted +
            " rejectedReason=" + (string.IsNullOrWhiteSpace(reason) ? "(none)" : reason) +
            " boostState=" + applyData.boostState
        );
    }

    /// <summary>
    /// Writes shock trap validation details to the Unity Console for debugging and validation.
    /// </summary>
    private void LogShockTrapValidation(string label, OnlineCardRequestData request, OnlineCardApplyData applyData, string reason)
    {
        if (request == null || applyData == null)
        {
            return;
        }

        Debug.Log(
            label +
            " actorPlayerId=" + request.actorPlayerId +
            " targetNodeId=" + request.targetId +
            " trapId=" + applyData.trapId +
            " accepted=" + applyData.accepted +
            " rejectedReason=" + (string.IsNullOrWhiteSpace(reason) ? "(none)" : reason) +
            " trapState=" + applyData.trapState
        );
    }

    /// <summary>
    /// Builds a readable description of gate state for debug logging.
    /// </summary>
    private string DescribeGateState(GateFrameAnimation gate)
    {
        if (gate == null)
        {
            return "Missing";
        }

        return gate.IsBlocking() ? "Closed" : "Open";
    }

    /// <summary>
    /// Builds a readable description of boost state for debug logging.
    /// </summary>
    private string DescribeBoostState(CannonTower tower)
    {
        if (tower == null)
        {
            return "Missing";
        }

        if (tower.boostActive)
        {
            return "Active";
        }

        return tower.boostPendingForNextWave ? "PendingForNextWave" : "None";
    }

    /// <summary>
    /// Builds trap ID for Photon messages, room properties, or debug logs.
    /// </summary>
    private string BuildTrapId(int ownerPlayerId, string targetNodeId)
    {
        return "ShockTrap_P" + ownerPlayerId + "_" + (string.IsNullOrWhiteSpace(targetNodeId) ? "UnknownNode" : targetNodeId);
    }

    /// <summary>
    /// Coordinates tower targeting manager instance for Photon synchronization and local scene state.
    /// </summary>
    private TowerTargetingManager TowerTargetingManagerInstance()
    {
        return FindObjectOfType<TowerTargetingManager>();
    }

    /// <summary>
    /// Coordinates shock trap targeting manager instance for Photon synchronization and local scene state.
    /// </summary>
    private ShockTrapTargetingManager ShockTrapTargetingManagerInstance()
    {
        return FindObjectOfType<ShockTrapTargetingManager>();
    }

    /// <summary>
    /// Looks up the target for actor number for player ID and applies the resolved gameplay result.
    /// </summary>
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

    /// <summary>
    /// Looks up the target for player ID for actor number and applies the resolved gameplay result.
    /// </summary>
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
                /// <summary>
                /// Returns Photon player int property needed by this gameplay system.
                /// </summary>
                return GetPhotonPlayerIntProperty(photonPlayer, PhotonLobbyPropertyKeys.PlayerId, -1);
            }
        }

        return -1;
#endif
    }

    /// <summary>
    /// Returns Photon player int property from Photon, room data, or the online player cache.
    /// </summary>
    private int GetPhotonPlayerIntProperty(Player photonPlayer, string key, int fallbackValue)
    {
        if (photonPlayer == null ||
            photonPlayer.CustomProperties == null ||
            !photonPlayer.CustomProperties.ContainsKey(key))
        {
            return fallbackValue;
        }

        /// <summary>
        /// Handles convert to int for Photon online card sync manager.
        /// </summary>
        return ConvertToInt(photonPlayer.CustomProperties[key], fallbackValue);
    }

    /// <summary>
    /// Converts to int into the expected value type and falls back safely if needed.
    /// </summary>
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

    /// <summary>
    /// Parses request JSON into the matching runtime sync data object.
    /// </summary>
    private OnlineCardRequestData DeserializeRequest(string json)
    {
        return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<OnlineCardRequestData>(json);
    }

    /// <summary>
    /// Parses apply JSON into the matching runtime sync data object.
    /// </summary>
    private OnlineCardApplyData DeserializeApply(string json)
    {
        return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<OnlineCardApplyData>(json);
    }

    /// <summary>
    /// Parses private state JSON into the matching runtime sync data object.
    /// </summary>
    private OnlinePrivateHandStateData DeserializePrivateState(string json)
    {
        return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<OnlinePrivateHandStateData>(json);
    }

    /// <summary>
    /// Handles build hand count map for Photon online card sync manager.
    /// </summary>
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

    /// <summary>
    /// Formats snapshot counts into readable text for UI or debug logs.
    /// </summary>
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

    /// <summary>
    /// Checks the current state to decide whether known empty player slot is true.
    /// </summary>
    private bool IsKnownEmptyPlayerSlot(PlayerResource player)
    {
        if (player == null)
        {
            return true;
        }

        return string.Equals(player.displayName, "Empty", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Registers Photon callbacks if needed so later callbacks, lookups, or sync messages can use it.
    /// </summary>
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

    /// <summary>
    /// Unregisters Photon callbacks if needed so old callbacks or duplicate listeners cannot fire.
    /// </summary>
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

    /// <summary>
    /// Checks whether run online game scene card sync is allowed before enabling that action.
    /// </summary>
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
