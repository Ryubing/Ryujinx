using System;

namespace Ryujinx.Graphics.Device
{
    /// <summary>
    /// Synchronization manager interface.
    /// </summary>
    public interface ISynchronizationManager
    {
        /// <summary>
        /// Increment the value of a syncpoint with a given id.
        /// </summary>
        /// <param name="id">The id of the syncpoint</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when id >= MaxHardwareSyncpoints</exception>
        /// <returns>The incremented value of the syncpoint</returns>
        uint IncrementSyncpoint(uint id);

        /// <summary>
        /// Get the value of a syncpoint with a given id.
        /// </summary>
        /// <param name="id">The id of the syncpoint</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when id >= MaxHardwareSyncpoints</exception>
        /// <returns>The value of the syncpoint</returns>
        uint GetSyncpointValue(uint id);

        /// <summary>
        /// Wait on a syncpoint with a given id at a target threshold.
        /// The callback will be called once the threshold is reached and will automatically be unregistered.
        /// </summary>
        /// <param name="id">The id of the syncpoint</param>
        /// <param name="threshold">The target threshold</param>
        /// <param name="timeout">The timeout</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when id >= MaxHardwareSyncpoints</exception>
        /// <returns>True if timed out</returns>
        bool WaitOnSyncpoint(uint id, uint threshold, TimeSpan timeout);
    }
}
