# Project TODO List

This file tracks all TODOs identified throughout the codebase, as well as planned features that have not yet been fully implemented or documented with a TODO comment.


## Marked TODO's

### Assets/Authenticate/PlayerAuthenticator.cs
- [ ] Line 321: Let Ronald look at this when I implemented security

### Assets/Core/Player/PlayerData.cs
- [ ] Line 52: Fishao used bezier curves for it's line, I might need to do the same
- [ ] Line 428: Make a function for in the syncManager, this should do the DB calls

### Assets/Core/Managers/GameNetworkManager.cs
- [ ] Line 136: Set all other character values like clothes
- [ ] Line 401: Add clothes that the player wants to wear, and check on the server if this is possible

## Mirror Networking Library

### Assets/Mirror/Transports/Threaded/ThreadedTransport.cs
- [ ] Line 133: nonalloc
- [ ] Line 139: nonalloc
- [ ] Line 205: Recycle writers
- [ ] Line 213: Deadlock protection. worker thread may be to slow to process all
- [ ] Line 688: Pass address in OnConnected
- [ ] Line 742: Cleaner

### Assets/Mirror/Transports/SimpleWeb/SimpleWeb/Server/WebSocketServer.cs
- [ ] Line 77: Keep track of connections before they are in connections dictionary

### Assets/Mirror/Transports/SimpleWeb/SimpleWeb/Common/ReceiveLoop.cs
- [ ] Line 131: Cache this to avoid allocations

### Assets/Mirror/Transports/KCP/ThreadedKcpTransport.cs
- [ ] Line 250: Not thread safe
- [ ] Line 295: Not thread safe

### Assets/Mirror/Transports/Encryption/EncryptionCredentials.cs
- [ ] Line 30: Load from file

### Assets/Mirror/Transports/Edgegap/EdgegapRelay/EdgegapKcpServer.cs
- [ ] Line 36: Don't call base. don't listen to local UdpServer at all?
- [ ] Line 86: Need separate buffer. don't write into result yet. only payload

### Assets/Mirror/Transports/KCP/kcp2k/highlevel/KcpPeer.cs
- [ ] Line 448: KcpConnection is IO agnostic. move this to outside later
- [ ] Line 497: KcpConnection is IO agnostic. move this to outside later
- [ ] Line 639: Buffer size check
- [ ] Line 769: KcpConnection is IO agnostic. move this to outside later

### Assets/Mirror/Transports/Edgegap/EdgegapLobby/Models/LobbyCreateRequest.cs
- [ ] Line 20: Annotations implementation

### Assets/Mirror/Transports/KCP/kcp2k/highlevel/KcpServer.cs
- [ ] Line 326: This allocates a new KcpConnection for each new

### Assets/Mirror/Components/RemoteStatistics.cs
- [ ] Line 112: Only load once, not for all players?
- [ ] Line 208: ServerTransport.earlyUpdateDuration.average
- [ ] Line 209: ServerTransport.lateUpdateDuration.average
- [ ] Line 313: Uptime display
- [ ] Line 400: Unspecified

### Assets/Mirror/Core/Batching/Batcher.cs
- [ ] Line 165: Safely get & return a batch instead of copying to writer?
- [ ] Line 166: Could return pooled writer & use GetBatch in a 'using' statement!

### Assets/Mirror/Components/PredictedRigidbody/PredictedRigidbody.cs
- [ ] Line 408: Unspecified
- [ ] Line 432: Maybe merge the IsMoving() checks & callbacks with UpdateState()
- [ ] Line 538: Maybe don't reuse the correction thresholds?
- [ ] Line 828: Maybe we should interpolate this back to 'now'?
- [ ] Line 854: Only position for now. consider rotation etc. too later
- [ ] Line 890: We should use the one from FixedUpdate

### Assets/Mirror/Hosting/Edgegap/Editor/EdgegapBuildUtils.cs
- [ ] Line 402: Use --password-stdin for security (!) This is no easy task for child Process

### Assets/Mirror/Components/LagCompensation/LagCompensator.cs
- [ ] Line 140: Consider rotations???
- [ ] Line 141: Consider original collider shape??
- [ ] Line 176: Rotation??
- [ ] Line 177: Different collier types??

### Assets/Mirror/Components/LagCompensation/HistoryCollider.cs
- [ ] Line 65: Double check
- [ ] Line 94: Projection?
- [ ] Line 97: Runtime drawing for debugging?

### Assets/Mirror/Core/LagCompensation/LagCompensation.cs
- [ ] Line 65: Faster version: guess start index by how many 'intervals' we are behind

### Assets/Mirror/Components/NetworkTransform/NetworkTransformBase.cs
- [ ] Line 30: This field is kind of unnecessary since we now support child NetworkBehaviours
- [ ] Line 203: target.lossyScale = scale
- [ ] Line 294: What about host mode?
- [ ] Line 298: The teleported client should ignore the rpc though
- [ ] Line 301: Or not? if client ONLY calls Teleport(pos), the position
- [ ] Line 316: What about host mode?
- [ ] Line 320: The teleported client should ignore the rpc though
- [ ] Line 323: Or not? if client ONLY calls Teleport(pos), the position
- [ ] Line 341: What about host mode?
- [ ] Line 357: What about host mode?
- [ ] Line 393: Unspecified
- [ ] Line 418: Unspecified

### Assets/Mirror/Core/LocalConnectionToServer.cs
- [ ] Line 90: Remove redundant state. have one source of truth for .ready!
- [ ] Line 103: Should probably be in connectionToClient.DisconnectInternal

### Assets/Mirror/Core/LagCompensation/HistoryBounds.cs
- [ ] Line 122: Technically we could reuse 'currentBucket' before clearing instead of encapsulating again

### Assets/Mirror/Components/Experimental/NetworkLerpRigidbody.cs
- [ ] Line 94: Does this also need to sync acceleration so and update velocity?

### Assets/Mirror/Components/NetworkTransform/NetworkTransformHybrid.cs
- [ ] Line 34: But use built in syncInterval instead of the extra field here!
- [ ] Line 451: Unspecified
- [ ] Line 468: Unspecified
- [ ] Line 486: What about host mode?
- [ ] Line 502: What about host mode?
- [ ] Line 515: What about host mode?
- [ ] Line 519: The teleported client should ignore the rpc though
- [ ] Line 522: Or not? if client ONLY calls Teleport(pos), the position
- [ ] Line 537: What about host mode?
- [ ] Line 541: The teleported client should ignore the rpc though
- [ ] Line 544: Or not? if client ONLY calls Teleport(pos), the position

### Assets/Mirror/Components/NetworkTransform/NetworkTransformReliable.cs
- [ ] Line 205: Dirty mask? [compression is very good w/o it already]

### Assets/Mirror/Core/NetworkBehaviour.cs
- [ ] Line 104: Change to NetworkConnectionToServer, but might cause some breaking
- [ ] Line 142: 64 SyncLists are too much. consider smaller mask later
- [ ] Line 486: Change conn type to NetworkConnectionToClient to begin with

### Assets/Mirror/Core/NetworkBehaviourHybrid.cs
- [ ] Line 254: Send same time that NetworkServer sends time snapshot?

### Assets/Mirror/Components/NetworkTransform/NetworkTransformUnreliable.cs
- [ ] Line 116: Send same time that NetworkServer sends time snapshot?

### Assets/Mirror/Core/NetworkClient.cs
- [ ] Line 58: Redundant state. point it to .connection.isReady instead (& test)
- [ ] Line 59: OR remove NetworkConnection.isReady? unless it's used on server
- [ ] Line 61: Maybe ClientState.Connected/Ready/AddedPlayer/etc.?
- [ ] Line 219: Why are there two connect host methods?
- [ ] Line 243: Move to 'cleanup' code below if safe
- [ ] Line 641: Likely not necessary anymore due to the new check in
- [ ] Line 722: Why do we have one with SpawnDelegate and one with SpawnHandlerDelegate?
- [ ] Line 736: Why do we have one with SpawnDelegate and one with SpawnHandlerDelegate?
- [ ] Line 777: Why do we have one with SpawnDelegate and one with SpawnHandlerDelegate?
- [ ] Line 849: Why do we have one with SpawnDelegate and one with SpawnHandlerDelegate?
- [ ] Line 1046: This is redundant. have one source of truth for .ready
- [ ] Line 1069: This check might not be necessary
- [ ] Line 1536: Set .connectionToServer to null for old local player?

### Assets/Mirror/Core/NetworkConnection.cs
- [ ] Line 20: Move this to ConnectionToClient so the flag only lives on server
- [ ] Line 66: If we only have Reliable/Unreliable, then we could initialize

### Assets/Mirror/Core/NetworkConnectionToClient.cs
- [ ] Line 25: Move to server's NetworkConnectionToClient?
- [ ] Line 34: Move them along server's timeline in the future
- [ ] Line 139: It would be safer for the server to store the last N

### Assets/Mirror/Editor/Weaver/EntryPointILPostProcessor/ILPostProcessorLogger.cs
- [ ] Line 16: Add file etc. for double click opening later?
- [ ] Line 27: IN-44868 FIX IS IN 2021.3.32f1, 2022.3.11f1, 2023.2.0b13 and 2023.3.0a8

### Assets/Mirror/Core/NetworkConnectionToServer.cs
- [ ] Line 18: Remove redundant state. have one source of truth for .ready!

### Assets/Mirror/Hosting/Edgegap/Editor/EdgegapWindowV2.cs
- [ ] Line 257: Load persistent data?
- [ ] Line 819: This will later be a result model
- [ ] Line 2451: Append "?{EdgegapWindowMetadata.DEFAULT_UTM_TAGS}"

### Assets/Mirror/Core/NetworkClient_TimeInterpolation.cs
- [ ] Line 10: Expose the settings to the user later

### Assets/Mirror/Core/NetworkIdentity.cs
- [ ] Line 176: Change to NetworkConnectionToServer, but might cause some breaking
- [ ] Line 696: Report this to Unity!


## Planned features

### Authentication & Session Management
- [ ] **Multi-device logout**: Implement functionality to automatically log out users from other tabs/devices when they log in on a new device or tab
  - Ensures only one active session per user at a time
  - Improves security by preventing simultaneous logins

### Events System
- [ ] **Automatic event creation**: Develop a system that automatically generates and schedules new in-game events
  - Should create events on a regular basis
  - Events should be varied and engaging for players

### Daily Missions & NPCs
- [ ] **Daily quest NPC**: Create a roaming NPC system that spawns in different locations each day
  - NPC should offer daily missions to players
  - Missions should reward players with fishcoins
  - Example mission: "Catch 3 sardines for 3 fishcoins"
  - NPC location should rotate daily to encourage exploration

### Achievements & Collections
- [ ] **Achievement system**: Implement a comprehensive achievements/collections feature
  - Size-based achievements: "Catch a fish larger than 100cm"
  - Location-based achievements: "Catch all fish species available at the beach"
  - Rarity-based achievements: "Catch all 1-star fish"
  - Track player progress and display collection status
  - Provide rewards for completing achievements
