using Ryujinx.Graphics.Device;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Host1x
{
    class SyncptIncrManager
    {
        private readonly ISynchronizationManager _syncMgr;

        private readonly struct SyncptIncr
        {
            public uint Id { get; }
            public ClassId ClassId { get; }
            public uint SyncptId { get; }
            public bool Done { get; }

            public SyncptIncr(uint id, ClassId classId, uint syncptId, bool done = false)
            {
                Id = id;
                ClassId = classId;
                SyncptId = syncptId;
                Done = done;
            }
        }

        private readonly List<SyncptIncr> _incrs = [];

        private uint _currentId;

        public SyncptIncrManager(ISynchronizationManager syncMgr)
        {
            _syncMgr = syncMgr;
        }

        public void Increment(uint id)
        {
            lock (_incrs)
            {
                _incrs.Add(new SyncptIncr(0, 0, id, true));

                IncrementAllDone();
            }
        }

        public uint IncrementWhenDone(ClassId classId, uint id)
        {
            lock (_incrs)
            {
                uint handle = _currentId++;

                _incrs.Add(new SyncptIncr(handle, classId, id));

                return handle;
            }
        }

        public void SignalDone(uint handle)
        {
            lock (_incrs)
            {
                // Set pending increment with the given handle to "done".
                for (int i = 0; i < _incrs.Count; i++)
                {
                    SyncptIncr incr = _incrs[i];

                    if (_incrs[i].Id == handle)
                    {
                        _incrs[i] = new SyncptIncr(incr.Id, incr.ClassId, incr.SyncptId, true);

                        break;
                    }
                }

                IncrementAllDone();
            }
        }

        private void IncrementAllDone()
        {
            lock (_incrs)
            {
                // Increment all sequential pending increments that are already done.
                int doneCount = 0;

                for (; doneCount < _incrs.Count; doneCount++)
                {
                    if (!_incrs[doneCount].Done)
                    {
                        break;
                    }

                    _syncMgr.IncrementSyncpoint(_incrs[doneCount].SyncptId);
                }

                _incrs.RemoveRange(0, doneCount);
            }
        }
    }
}
