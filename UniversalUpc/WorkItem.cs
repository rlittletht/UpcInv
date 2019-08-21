
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UniversalUpc;

namespace UniversalUpc
{
    public class WorkItemView
    {
        public int WorkId { get; }
        public string ScanCode { get; }
        public WorkItem.Status CurrentStatus { get; }

        public WorkItemView(int workId, string scanCode, WorkItem.Status currentStatus)
        {
            WorkId = workId;
            ScanCode = scanCode;
            CurrentStatus = currentStatus;
        }
    }

    public class WorkItem
    {
        public enum Status
        {
            Created,
            Waiting,
            Dispatched,
            Complete,
            Failed
        }

        public static int s_MaxStage = 10;

        public string ScanCode { get; set; }
        public int WorkId { get; set; }
        public DateTime ScanTime { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime FinishTime { get; set; }
        public Status CurrentStatus { get; set; }
        public long[] StageTime { get; set; }
        public int StageMax { get; set; }
        public Stopwatch Timer { get; set; }
        public long TimerFrequency { get; set; }

        private WorkBoard.WorkItemDispatch WorkDelegate { get; set; }

        public WorkItem(string scanCode, WorkBoard.WorkItemDispatch del, int workId)
        {
            WorkDelegate = del;
            WorkId = workId;
            ScanCode = scanCode;
            StartTime = DateTime.Now;
            StageMax = 0;
            Timer = new Stopwatch();
            TimerFrequency = Stopwatch.Frequency;
            Timer.Start();
            StageTime = new long[s_MaxStage];
        }

        public WorkItemView GetView()
        {
            return new WorkItemView(WorkId, ScanCode, CurrentStatus);
        }

        public async Task DoWork()
        {
            await WorkDelegate();
        }

        public int BumpStage()
        {
            Timer.Stop();

            if (StageMax >= s_MaxStage)
                return StageMax;

            StageTime[StageMax] = Timer.ElapsedMilliseconds;
            Timer.Reset();
            Timer.Start();

            return ++StageMax;
        }
    }


}