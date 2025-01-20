using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.IO;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            // Получаем текущий каталог приложения
            string currentDir = Directory.GetCurrentDirectory();

            // Создаем относительные пути к директориям
            string soundsDirName = Path.Combine(currentDir, "resources", "sounds");
            string normalSoundsDirName = Path.Combine(currentDir, "resources", "normalSounds");
            string quietSoundsDirName = Path.Combine(currentDir, "resources", "quietSounds");
            string mutedSoundsDirName = Path.Combine(currentDir, "resources", "mutedSounds");

            // Создаем выходные директории, если они не существуют
            Directory.CreateDirectory(normalSoundsDirName);
            Directory.CreateDirectory(quietSoundsDirName);
            Directory.CreateDirectory(mutedSoundsDirName);

            var sourceDir = new DirectoryInfo(soundsDirName);
            FileInfo[] files = sourceDir.GetFiles();
            foreach (var item in files)
            {
                string inputFilePath = Path.Combine(soundsDirName, item.Name);

                // Изменяем громкость для разных уровней
                ApplyAttackReleaseEffect(inputFilePath, normalSoundsDirName, item.Name, 0.75f, 0.001f, 0.5f);
                ApplyAttackReleaseEffect(inputFilePath, quietSoundsDirName, item.Name, 0.5f, 0.001f, 0.5f);
                ApplyAttackReleaseEffect(inputFilePath, mutedSoundsDirName, item.Name, 0.6f, 0.001f, 0.016f); //общая громкость, атака, релиз
            }
        }


        /*
        static void ChangeVolume(string inputFilePath, string outputDir, string fileName, float volume)
        {
            using (var reader = new AudioFileReader(inputFilePath))
            {
                var volumeProvider = new VolumeSampleProvider(reader) { Volume = volume };
                using (var waveFileWriter = new WaveFileWriter(Path.Combine(outputDir, fileName), reader.WaveFormat))
                {
                    var buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
                    int samplesRead;

                    while ((samplesRead = volumeProvider.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        waveFileWriter.WriteSamples(buffer, 0, samplesRead);
                    }
                }
            }
        }
        */
        static void ApplyAttackReleaseEffect(string inputFilePath, string outputDir, string fileName, float volume, float attackTime, float releaseTime)
        {
            using (var reader = new AudioFileReader(inputFilePath))
            {
                var volumeProvider = new VolumeSampleProvider(reader) { Volume = volume };
                var attackReleaseProvider = new AttackReleaseSampleProvider(volumeProvider, attackTime, releaseTime);
                using (var waveFileWriter = new WaveFileWriter(Path.Combine(outputDir, fileName), reader.WaveFormat))
                {
                    var buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
                    int samplesRead;

                    while ((samplesRead = attackReleaseProvider.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        waveFileWriter.WriteSamples(buffer, 0, samplesRead);
                    }
                }
            }
        }
    }

    public class AttackReleaseSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly float attackTime; // Время атаки в секундах
        private readonly float releaseTime; // Время релиза в секундах

        private float currentGain = 0f;
        private float targetGain = 0f;
        private float increment;
        private float decrement;

        public AttackReleaseSampleProvider(ISampleProvider source, float attackTime, float releaseTime)
        {
            this.source = source;
            this.attackTime = attackTime;
            this.releaseTime = releaseTime;

            // Вычисляем инкременты для атаки и релиза посэмплово
            increment = 1.0f / (attackTime * source.WaveFormat.SampleRate);
            decrement = 1.0f / (releaseTime * source.WaveFormat.SampleRate);
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = source.Read(buffer, offset, count);

            for (int i = 0; i < samplesRead; i++)
            {
                // Устанавливаем целевую громкость
                if (buffer[offset + i] > 0.05f) // Порог, выше которого звук считается "громким"
                {
                    targetGain = 1.0f; // Максимальная громкость
                }
                else
                {
                    targetGain = 0.0f; // Падение до нуля
                }

                // Управляем текущей громкостью
                if (currentGain < targetGain)
                {
                    currentGain += increment;
                    if (currentGain > targetGain)
                    {
                        currentGain = targetGain;
                    }
                }
                else
                {
                    currentGain -= decrement;
                    if (currentGain < targetGain)
                    {
                        currentGain = targetGain;
                    }
                }

                // Применяем текущую громкость к выходному сигналу
                buffer[offset + i] *= currentGain;
            }

            return samplesRead;
        }


    }
}