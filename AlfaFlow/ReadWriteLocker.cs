using System;
using System.Threading;


namespace AlfaFlow
{
	public interface IReadWriteLocker
	{
		void Enter();
		void Leave();
		void ReadEnter();
		void ReadLeave();
		void WriteEnter();
		void WriteLeave();
	}

	public struct ReadWriteLocker
	{
		[System.Runtime.TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		public void Enter()
		{
			do
			{
				if (m_lock == 0) Interlocked.Increment(ref m_lock);
			} while (m_lock != 1);
		}

		[System.Runtime.TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		public void Leave()
		{
			m_lock = 0;
		}

		[System.Runtime.TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		public void ReadEnter()
		{
			Enter();
			m_reads++;
			Leave();
		}

		[System.Runtime.TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		public void ReadLeave()
		{
			Enter();
			m_reads--;
			Leave();
		}

		[System.Runtime.TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		public void WriteEnter()
		{
			Enter();
			if (m_reads > 0) Leave();
		}

		[System.Runtime.TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		public void WriteLeave()
		{
			Leave();
		}

		int m_lock;
		volatile int m_reads;
	}
}
