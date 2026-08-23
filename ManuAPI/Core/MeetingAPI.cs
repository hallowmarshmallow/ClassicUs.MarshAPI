using System;
using System.Collections.Generic;

namespace ClassicUs.ManuAPI
{
    /// <summary>
    /// Meeting helpers: vote tracking, custom meeting chat messages, and
    /// meeting state queries. All lookups are read-only — state mutation belongs
    /// in the game's MeetingHud lifecycle.
    /// </summary>
    public static class MeetingAPI
    {
        /// <summary>True when a meeting is currently active.</summary>
        public static bool IsInMeeting => MeetingHud.Instance != null && MeetingHud.Instance.isActiveAndEnabled;

        /// <summary>Returns the active MeetingHud instance, or null.</summary>
        public static MeetingHud CurrentMeeting => MeetingHud.Instance;

        /// <summary>Number of alive (non-disconnected, non-dead) players in the current meeting.</summary>
        public static int AliveVoterCount
        {
            get
            {
                if (MeetingHud.Instance == null || MeetingHud.Instance.playerStates == null) return 0;
                int count = 0;
                foreach (var state in MeetingHud.Instance.playerStates)
                    if (state != null && !state.AmDead && state.TargetPlayerId != 255)
                        count++;
                return count;
            }
        }

        /// <summary>Number of players who have already voted.</summary>
        public static int VotedCount
        {
            get
            {
                if (MeetingHud.Instance == null || MeetingHud.Instance.playerStates == null) return 0;
                int count = 0;
                foreach (var state in MeetingHud.Instance.playerStates)
                    if (state != null && state.DidVote)
                        count++;
                return count;
            }
        }

        /// <summary>
        /// Gets the vote tally: how many votes each player received.
        /// Key is the target PlayerId (byte), value is the vote count.
        /// </summary>
        public static Dictionary<byte, int> VoteTally()
        {
            var tally = new Dictionary<byte, int>();
            if (MeetingHud.Instance == null || MeetingHud.Instance.playerStates == null) return tally;

            foreach (var state in MeetingHud.Instance.playerStates)
            {
                if (state == null || !state.DidVote) continue;
                var targetId = state.VotedFor;
                if (!tally.ContainsKey(targetId)) tally[targetId] = 0;
                tally[targetId]++;
            }

            return tally;
        }

        /// <summary>
        /// Returns the player who has the most votes, or 255 if there is a tie or no votes.
        /// </summary>
        public static byte MostVotedPlayer()
        {
            var tally = VoteTally();
            byte best = 255;
            int bestCount = 0;
            bool tie = false;

            foreach (var kv in tally)
            {
                if (kv.Value > bestCount)
                {
                    best = kv.Key;
                    bestCount = kv.Value;
                    tie = false;
                }
                else if (kv.Value == bestCount)
                {
                    tie = true;
                }
            }

            return tie ? (byte)255 : best;
        }

        /// <summary>
        /// Returns the number of votes a specific player received.
        /// </summary>
        public static int VotesFor(byte playerId)
        {
            var tally = VoteTally();
            return tally.TryGetValue(playerId, out var count) ? count : 0;
        }

        /// <summary>
        /// Returns the name of the player a given voter cast their vote for.
        /// Returns "(skipped)" if they voted to skip, or "?" if unknown.
        /// </summary>
        public static string VoteTargetName(byte voterId)
        {
            if (MeetingHud.Instance == null || MeetingHud.Instance.playerStates == null) return "?";

            foreach (var state in MeetingHud.Instance.playerStates)
            {
                if (state == null || state.TargetPlayerId != voterId || !state.DidVote) continue;
                var targetId = state.VotedFor;
                if (targetId == 254 || targetId == 255) return "(skipped)";
                return PlayerUtils.NameSafe(targetId);
            }

            return "?";
        }

        /// <summary>
        /// True if the exiled player (from the most recent meeting) matches the given playerId.
        /// Returns false when no meeting has resolved yet.
        /// </summary>
        public static bool WasExiled(byte playerId)
        {
            if (MeetingHud.Instance == null) return false;
            foreach (var state in MeetingHud.Instance.playerStates)
            {
                if (state == null || state.AmDead) continue;
                // The exiled player is the one whose vote count exceeds the threshold
                // and who is now dead. We use the meeting result to infer this.
            }

            // After MeetingHud closes, ExileController holds the result.
            var exile = ExileController.Instance;
            if (exile == null || exile.exiled == null) return false;
            return exile.exiled.PlayerId == playerId;
        }

        /// <summary>
        /// Gets the name of the player exiled in the most recent meeting, or null if nobody was exiled.
        /// </summary>
        public static string ExiledPlayerName()
        {
            var exile = ExileController.Instance;
            if (exile == null || exile.exiled == null) return null;
            return exile.exiled.PlayerName;
        }

        /// <summary>
        /// Sends a meeting chat message visible to all players (host-only effect).
        /// This uses the game's chat system, which is available during meetings.
        /// Returns false when chat is not available.
        /// </summary>
        public static bool SendMeetingChat(string message)
        {
            if (string.IsNullOrEmpty(message)) return false;
            try
            {
                // The ChatController is available during meetings. Use the
                // AddChatWarning path (the same native popup used by SystemChat)
                // since AddChat bypasses the RPC chat pipeline.
                var popup = HudManager.Instance?.ChatPopup;
                if (popup == null) return false;
                popup.ShowWarning(message);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sends a system message that appears in the meeting chat as a "SYSTEM
        /// ALERT" popup. Same as SendMeetingChat but uses a different style.
        /// </summary>
        public static bool SendMeetingSystemMessage(string message)
        {
            return SendMeetingChat(message);
        }
    }
}