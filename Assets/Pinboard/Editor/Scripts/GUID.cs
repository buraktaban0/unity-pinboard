using System;

namespace Pinboard
{
	public static class Guid
	{
		private const string RANGE = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";

		private const int UID_LENGTH = 32;

		private static readonly char[] CHARS = RANGE.ToCharArray();

		private static readonly System.Random RAND = new Random();

		private static readonly char[] buffer = new char[UID_LENGTH];


		public static string Get()
		{
			return System.Guid.NewGuid().ToString();

			int len = CHARS.Length;
			for (int i = 0; i < UID_LENGTH; i++)
			{
				buffer[i] = CHARS[RAND.Next(0, len)];
			}

			return new string(buffer);
		}
	}
}
