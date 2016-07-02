﻿using System;
using System.Diagnostics;
using System.IO;

using IrrKlang;

using Sagen;
using Sagen.Samplers;

using SampleFormat = Sagen.SampleFormat;

namespace HaarpConsole
{
	class Program
	{
		public static ISoundEngine Engine = new ISoundEngine(SoundOutputDriver.AutoDetect, SoundEngineOptionFlag.MultiThreaded);

		static void Main(string[] args)
		{
			var synth = new Synthesizer { Fundamental = 155f };

			const float amp = .015f;
			const float tilt = -3.00f;

			// Generate 100 harmonics
			for (int i = 0; i < 100; i++)
				synth.AddSampler(new HarmonicSampler(synth, i, amp, .14f * i, tilt));
			synth.AddSampler(new VocalSampler(synth, 0));

			var sound = new MemoryStream(10000);
			var sw = new Stopwatch();

			Console.WriteLine("Generating...");
			sw.Start();
			synth.Generate(4.0f, sound, SampleFormat.Signed16);
			sw.Stop();
			Console.WriteLine($"Finished in {sw.Elapsed}");

			// Write sound to file
			using (var file = File.Create("SpeechOutput.wav"))
			{
				sound.WriteTo(file);
				file.Flush();
			}

			Play(sound);

			Console.ReadLine();
		}

		private class StopEventReceiver : ISoundStopEventReceiver
		{
			public readonly Action<ISound, StopEventCause> StopAction;

			public StopEventReceiver(Action<ISound, StopEventCause> action)
			{
				StopAction = action;
			}

			public void OnSoundStopped(ISound sound, StopEventCause reason, object userData)
			{
				StopAction?.Invoke(sound, reason);
			}
		}

		public static void Play(MemoryStream wavAudioStream)
		{
			wavAudioStream.Seek(0, SeekOrigin.Begin);
			var src = Engine.AddSoundSourceFromIOStream(wavAudioStream, "snd");
			Console.WriteLine("Play length: {0}s", (float)src.PlayLength / 1000);
			var snd = Engine.Play2D(src, false, false, false);
			snd.setSoundStopEventReceiver(new StopEventReceiver((sound, cause) =>
			{
				src.Dispose();
			}));
		}
	}
}