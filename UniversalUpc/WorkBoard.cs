
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using TCore.Logging;

namespace UniversalUpc
{
    /// <summary>
    /// Holds all workitems (past, current, and pending)
    /// </summary>
    public class WorkBoard
    {
        private object m_oLock = new Object();
        private UpdateWorkBoard m_delUpdateBoard;


        private int m_workIdNext = 0;
        private Dictionary<int, WorkItem> m_board = new Dictionary<int, WorkItem>();

        public delegate Task WorkItemDispatch();

        public delegate void UpdateWorkBoard(WorkItemView view);

        public WorkBoard(UpdateWorkBoard delUpdateBoard)
        {
            m_delUpdateBoard = delUpdateBoard;
        }

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

        public void SetWorkDelegate(int workId, WorkItemDispatch dispatch)
        {
            lock (m_oLock)
            {
                WorkItem work = m_board[workId];

                work.SetWorkDelegate(dispatch);
            }
        }

        public void UpdateWork(int workId, bool fResult, string sTitle)
        {
            lock (m_oLock)
            {
                WorkItem work = m_board[workId];
                work.Description = sTitle;
                work.Succeeded = fResult;
            }
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

        public async Task DoWorkItem(int workId)
        {
            WorkItem work;
            WorkItemView view;

            lock (m_oLock)
            {
                work = m_board[workId];

                work.BumpStage();
                work.CurrentStatus = WorkItem.Status.Dispatched;
                view = work.GetView();
            }

            m_delUpdateBoard(view);

            await work.DoWork();

            lock(m_oLock)
            {
                work = m_board[workId];
                work.BumpStage();
                work.CurrentStatus = work.Succeeded ? WorkItem.Status.Complete : WorkItem.Status.Failed;
                view = work.GetView();
            }

            m_delUpdateBoard(view);
        }
    }
}
