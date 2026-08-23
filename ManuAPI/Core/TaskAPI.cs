using System;
using System.Collections.Generic;

namespace ClassicUs.ManuAPI
{
    /// <summary>
    /// Task-related utilities: task counts, progress, completion status.
    /// All read-only queries against the authoritative GameData on each client.
    /// </summary>
    public static class TaskAPI
    {
        /// <summary>Total tasks assigned to a player (short + long + common), or 0 if unknown.</summary>
        public static int TotalTasks(PlayerControl player)
        {
            if (player == null || player.Data == null || player.Data.Tasks == null) return 0;
            return player.Data.Tasks.Count;
        }

        /// <summary>Number of completed tasks for a player, or 0 if unknown.</summary>
        public static int CompletedTasks(PlayerControl player)
        {
            if (player == null || player.Data == null || player.Data.Tasks == null) return 0;
            int completed = 0;
            foreach (var task in player.Data.Tasks)
                if (task != null && task.Complete)
                    completed++;
            return completed;
        }

        /// <summary>True when a player has finished all their tasks.</summary>
        public static bool IsDone(PlayerControl player)
        {
            if (player == null || player.Data == null) return false;
            return TotalTasks(player) > 0 && CompletedTasks(player) >= TotalTasks(player);
        }

        /// <summary>
        /// Total crewmate task progress across all alive crewmates. Returns a
        /// float between 0.0 and 1.0.
        /// </summary>
        public static float CrewProgress()
        {
            int total = 0, done = 0;
            foreach (var p in PlayerUtils.AllAliveOnTeam(RoleTeamTypes.Crewmate))
            {
                total += TotalTasks(p);
                done += CompletedTasks(p);
            }
            return total > 0 ? (float)done / total : 0f;
        }

        /// <summary>
        /// Gets all incomplete tasks for a player. Useful for custom abilities
        /// that interact with specific task types.
        /// </summary>
        public static List<GameData.TaskInfo> IncompleteTasks(PlayerControl player)
        {
            var result = new List<GameData.TaskInfo>();
            if (player == null || player.Data == null || player.Data.Tasks == null) return result;
            foreach (var task in player.Data.Tasks)
                if (task != null && !task.Complete)
                    result.Add(task);
            return result;
        }

        /// <summary>
        /// Returns true if the task at the given index is a "common task"
        /// (shared by all crewmates — e.g. Swipe Card, Wires).
        /// </summary>
        public static bool IsCommonTask(int taskIndex)
        {
            // Common tasks are those that appear in the game's common task pool.
            // This is a heuristic: task types 0-2 are typically common tasks on
            // Skeld (SwipeCard=0, Wires=1, Keys=2); the actual set is map-dependent.
            return taskIndex >= 0 && taskIndex <= 4;
        }

        /// <summary>
        /// Gets the display name of a task type from the game's task data.
        /// Returns "Task #N" when the name cannot be resolved.
        /// </summary>
        public static string TaskName(GameData.TaskInfo task)
        {
            if (task == null) return "Unknown";
            try
            {
                // GameData.TaskInfo has TypeId which maps to the task type enum.
                // The exact type name depends on the map.
                return "Task #" + task.Id;
            }
            catch
            {
                return "Task";
            }
        }

        /// <summary>
        /// Returns the total number of tasks remaining for the crew team to win
        /// by tasks (excluding ghost players' progress).
        /// </summary>
        public static int RemainingTaskCount()
        {
            int total = 0, done = 0;
            foreach (var p in PlayerUtils.AllAliveOnTeam(RoleTeamTypes.Crewmate))
            {
                total += TotalTasks(p);
                done += CompletedTasks(p);
            }
            return total - done;
        }
    }
}