using System;
using System.IO;
using System.Linq;

namespace Yaw.Core.Utils.IO
{
	/// <summary>
	/// Содержит вспомогательные методы для работы с файлами
	/// </summary>
	public class FileUtils
	{
		/// <summary>
		/// Число байт в килобайте
		/// </summary>
		public const int BYTES_IN_KB = 1024;
		/// <summary>
		/// Число байт в мегабайте
		/// </summary>
		public const int BYTES_IN_MB = BYTES_IN_KB * BYTES_IN_KB;

        /// <summary>
        /// В папке <paramref name="folder"/> создает новый файл с уникальным именем и открывает его на запись.
        /// Имя файла формируется как <paramref name="fileNamePrefix"/> + {DT}.{N} + <paramref name="fileExtension"/>,
        /// где 
        /// {DT} - временнАя метка, имеющая формат yyyyMMdd
        /// {N} - уникальный номер файла. {N} последовательно возрастает при создании каждого нового файла.
        /// </summary>
        /// <param name="folder">Папка, в которой создается файл.</param>
        /// <param name="fileNamePrefix">Начало имени файла.</param>
        /// <param name="fileExtension">Расширение файла.</param>
        /// <param name="fileNumberLength">Число знакомест в номере файла. Если номер файла занимает меньше знакомест, то слева добавляются нули.</param>
        /// <returns>Поток для записи в файл.</returns>
        public static FileStream CreateUniqueFileWithDateMark(
            String folder, String fileNamePrefix, String fileExtension, Int32 fileNumberLength)
        {
            return CreateUniqueFile(
                folder,
                String.Format("{0}.{1:yyyyMMdd}.", fileNamePrefix, DateTime.Now),
                fileExtension,
                fileNumberLength);
        }

	    /// <summary>
		/// В папке <paramref name="folder"/> создает новый файл с уникальным именем и открывает его на запись.
		/// Имя файла формируется как <paramref name="fileNamePrefix"/> + {N} + <paramref name="fileExtension"/>,
		/// где {N} - уникальный номер файла. {N} последовательно возрастает при создании каждого нового файла.
		/// </summary>
		/// <param name="folder">Папка, в которой создается файл.</param>
		/// <param name="fileNamePrefix">Начало имени файла.</param>
		/// <param name="fileExtension">Расширение файла.</param>
		/// <param name="fileNumberLength">Число знакомест в номере файла. Если номер файла занимает меньше знакомест, то слева добавляются нули.</param>
		/// <returns>Поток для записи в файл.</returns>
		public static FileStream CreateUniqueFile(
			String folder, String fileNamePrefix, String fileExtension, Int32 fileNumberLength)
		{
			const Int32 MAX_TRIES = 3;

			Int32 tryCount = 0;
			while (true)
			{
				try
				{
					String fileName = GetUniqueName(folder, fileNamePrefix, fileExtension, fileNumberLength);

					return new FileStream(
						Path.Combine(folder, fileName),
						FileMode.CreateNew,
						FileAccess.Write,
						FileShare.Read);
				}
				catch (IOException)
				{
					// Видимо, файл успели создать в промежутке между поиском файла и его созданием.
					// Попробуем еще раз (но не более MAX_TRIES раз).
					tryCount++;
					if (tryCount >= MAX_TRIES)
						throw;
				}
			}
		}

	    /// <summary>
	    /// Создает директорию с уникальным именем
	    /// </summary>
	    /// <param name="folder"></param>
	    /// <param name="folderNamePrefix"></param>
	    /// <param name="folderNumberLength"></param>
	    /// <returns>Путь к созданной директории</returns>
	    public static string CreateUniqueFolder(String folder, String folderNamePrefix, Int32 folderNumberLength)
		{
			const Int32 MAX_TRIES = 3;
			// добавим дату к префиксу папки
			folderNamePrefix = String.Format("{0}_{1:yyyyMMdd}.", folderNamePrefix, DateTime.Now);

			Int32 tryCount = 0;
			while (true)
			{
				try
				{
					String folderName = GetUniqueName(folder, folderNamePrefix, "", folderNumberLength);
					folderName = Path.Combine(folder, folderName);
					Directory.CreateDirectory(folderName);
					
					return folderName;
				}
				catch (IOException)
				{
					// Видимо, файл успели создать в промежутке между поиском папки и ее созданием.
					// Попробуем еще раз (но не более MAX_TRIES раз).
					tryCount++;
					if (tryCount >= MAX_TRIES)
						throw;
				}
			}
		}

		/// <summary>
		/// Формирует уникальное имя файла или папки
		/// </summary>
		/// <param name="folder">корневая папка</param>
		/// <param name="namePrefix">префикс имени</param>
		/// <param name="fileExtension">расширение файла</param>
		/// <param name="numberLength">Число знакомест в номере файла. Если номер файла занимает меньше знакомест, то слева добавляются нули.</param>
		/// <returns></returns>
		public static string GetUniqueName(
            String folder, String namePrefix, String fileExtension, Int32 numberLength)
		{
			if (!String.IsNullOrEmpty(fileExtension))
				fileExtension = "." + fileExtension;

			String searchPattern = namePrefix + new String('?', numberLength) + fileExtension;

			var files = Directory.GetFileSystemEntries(folder, searchPattern);
			String lastFile = files.Length == 0 ? null : files.Max();

			Int32 orderNumber = 0;
			if (lastFile != null)
			{
				String sLogNumber = Path.GetFileName(lastFile).Substring(namePrefix.Length, numberLength);
				Int32.TryParse(sLogNumber, out orderNumber);
			}
			orderNumber++;

			String name =
				namePrefix +
				orderNumber.ToString(new String('0', numberLength)) +
				fileExtension;

			return name;
		}

		/// <summary>
		/// Вычисляет относительный путь для <paramref name="path"/> по отношению к папке <paramref name="folder"/>
		/// </summary>
		/// <param name="folder">Путь к папке, относительно которой вычисляется путь</param>
		/// <param name="path">Путь к файлу или папке</param>
		/// <returns></returns>
		public static String GetRelativePath(String folder, String path)
		{
			if (string.IsNullOrEmpty(path))
				return String.Empty;

			if (!Path.IsPathRooted(path))
				return path;

			var lastChar = folder[folder.Length - 1];
			if (lastChar != Path.DirectorySeparatorChar && lastChar != Path.AltDirectorySeparatorChar)
				folder += Path.DirectorySeparatorChar;

			var uri1 = new Uri(folder);
			var uri2 = new Uri(path);
			var relPath = Uri.UnescapeDataString(uri1.MakeRelativeUri(uri2).ToString());
			return relPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		}

		/// <summary>
		/// Проверяет существует ли директория <paramref name="path"/>, если нет, создает ее
		/// </summary>
		/// <param name="path">путь к директории</param>
		public static void EnsureDirExists(string path)
		{
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
		}

        /// <summary>
        /// Копирует директорию со всем ее содержимым
        /// </summary>
        /// <param name="sourceDirPath"></param>
        /// <param name="destDirPath"></param>
        public static void CopyDirectory(string sourceDirPath, string destDirPath)
        {
            if (!Directory.Exists(destDirPath))
            {
                Directory.CreateDirectory(destDirPath);
            }

            foreach (var file in Directory.GetFiles(sourceDirPath))
            {
                var name = Path.GetFileName(file);
                var dest = Path.Combine(destDirPath, name);
                File.Copy(file, dest);
            }

            foreach (var folder in Directory.GetDirectories(sourceDirPath))
            {
                var name = Path.GetFileName(folder);
                var dest = Path.Combine(destDirPath, name);
                CopyDirectory(folder, dest);
            }
        }
	}
}
