
using System;
using System.Collections.Generic;

namespace UniversalUpc
{
    /// <summary>
    /// Holds all workitems (past, current, and pending)
    /// </summary>
    public class WorkBoard
    {
        private object m_oLock = new Object();

        private int m_workIdNext = 0;
        private Dictionary<int, WorkItem> m_board = new Dictionary<int, WorkItem>();

        public delegate void WorkItemDispatch();

        /// <summary>
        /// Create a new WorkItem and add it to the board in the created state
        /// </summary>
        /// <returns></returns>
        public int CreateWork(string scanCode, WorkItemDispatch dispatch)
        {
            WorkItem newWork;
            lock (m_oLock)
            {
                newWork = new WorkItem(scanCode, dispatch, m_workIdNext++)
                {
                    CurrentStatus = WorkItem.Status.Created
                };

                m_board.Add(newWork.WorkId, newWork);
            }

            return newWork.WorkId;
        }

        public WorkItemView GetWorkItemView(int workId)
        {
            WorkItemView view; 

            lock (m_oLock)
            {
                view = m_board[workId].GetView();
            }

            return view;
        }

        public void DoWorkItem(int workId)
        {
            WorkItem work;

            lock (m_oLock)
            {
                work = m_board[workId];

                work.BumpStage();
            }

            work.DoWork();

            lock(m_oLock)
            {
                work = m_board[workId];
                work.BumpStage();
            }
        }
    }
}
