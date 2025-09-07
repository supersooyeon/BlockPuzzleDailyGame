using BlockPuzzleGameToolkit.Scripts.Data;
using BlockPuzzleGameToolkit.Scripts.Enums;

namespace BlockPuzzleGameToolkit.Scripts.System
{
	public static class HighScoreService
	{
		public static int GetBest(EGameMode mode)
		{
			var resource = ResourceManager.instance.GetResource(GetKey(mode));
			if (resource == null)
			{
				return 0;
			}

			int value = resource.GetValue();
			if (value <= 0)
			{
				// 백업 키에서 복원 시도
				int backup = UnityEngine.PlayerPrefs.GetInt(GetBackupKey(mode), 0);
				if (backup > value)
				{
					resource.Set(backup);
					return backup;
				}
			}

			return value;
		}

		public static bool TryUpdateBest(EGameMode mode, int newScore)
		{
			var resource = ResourceManager.instance.GetResource(GetKey(mode));
			if (resource == null)
			{
				return false;
			}

			int current = resource.GetValue();
			if (newScore > current)
			{
				resource.Set(newScore);
				// 백업 키에도 동시 저장하여 업데이트 시 초기화 방지
				UnityEngine.PlayerPrefs.SetInt(GetBackupKey(mode), newScore);
				UnityEngine.PlayerPrefs.Save();
				return true;
			}

			return false;
		}

		public static int SyncFromState(EGameMode mode, int stateBestScore)
		{
			if (stateBestScore <= 0)
			{
				return GetBest(mode);
			}

			var resource = ResourceManager.instance.GetResource(GetKey(mode));
			if (resource == null)
			{
				return stateBestScore;
			}

			int current = resource.GetValue();
			if (stateBestScore > current)
			{
				resource.Set(stateBestScore);
				return stateBestScore;
			}

			return current;
		}

		private static string GetKey(EGameMode mode)
		{
			switch (mode)
			{
				case EGameMode.Timed:
					return "TimedBestScore";
				case EGameMode.Classic:
			default:
					return "Score";
			}
		}

		private static string GetBackupKey(EGameMode mode)
		{
			switch (mode)
			{
				case EGameMode.Timed:
					return "TimedBestScoreBackup";
				case EGameMode.Classic:
			default:
					return "ScoreBackup";
			}
		}
	}
}

