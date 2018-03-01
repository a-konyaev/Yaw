using System;
using System.Configuration;
using System.IO;

namespace Yaw.Core.Configuration
{
    /// <summary>
    /// Конфиг-элемент, который содержит путь, 
    /// который может содержать шаблон, содержащий символы '*' и '?'
    /// </summary>
    public class PathPatternConfig : ConfigurationElement
    {
        /// <summary>
        /// Путь
        /// </summary>
        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get
            {
                //return (string) this["path"];
                return ResolvePath((string)this["path"]);
            }
            set
            {
                this["path"] = value;
            }
        }

        /// <summary>
        /// Разрешить путь, который может быть задан с использованием маски
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string ResolvePath(string path)
        {
            try
            {
                path = path.Replace('\\', '/');

                // определим индекс первого символа маски
                var i1 = path.IndexOf('*');
                var i2 = path.IndexOf('?');
                var firstPatternCharIndex = Math.Min(
                    i1 == -1 ? int.MaxValue : i1,
                    i2 == -1 ? int.MaxValue : i2);

                // если шаблон содержит маску
                if (firstPatternCharIndex != int.MaxValue)
                {
                    // определим индекс '/', который идет последним перед тем, как начинается маска
                    var slashIndex = path.Substring(0, firstPatternCharIndex + 1).LastIndexOf('/');
                    // получим часть пути, которая не содержит маски
                    var startPath = path.Substring(0, slashIndex);

                    // найдем директорию по маске
                    path = FindDirectory(startPath, path.Substring(startPath.Length + 1));
                }

                return path;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ошибка разрешения пути {0}: {1}", path, ex.Message));
            }
        }

        /// <summary>
        /// Найти полный путь к директории
        /// </summary>
        /// <param name="path">путь, начиная с которого нужно искать</param>
        /// <param name="searchPattern">шаблон поиска</param>
        /// <returns></returns>
        /// <remarks>
        /// Пример: FindDirectory("/var/autofs/removable", "mmcblk*/WorkData/*/Log")
        /// </remarks>
        private static string FindDirectory(string path, string searchPattern)
        {
            string[] resArr;

            // определим индекс первого символа маски
            var i1 = searchPattern.IndexOf('*');
            var i2 = searchPattern.IndexOf('?');
            var firstPatternCharIndex = Math.Min(
                i1 == -1 ? int.MaxValue : i1,
                i2 == -1 ? int.MaxValue : i2);

            // если шаблон не содержит маску
            if (firstPatternCharIndex == int.MaxValue)
            {
                return System.IO.Path.Combine(path, searchPattern);
            }

            var slashIndex = searchPattern.IndexOf('/');
            if (slashIndex == -1)
            {
                resArr = Directory.GetDirectories(path, searchPattern);
                if (resArr.Length == 0)
                    throw new Exception("Директория не найдена: " + System.IO.Path.Combine(path, searchPattern));

                return resArr[0];
            }

            var sp1 = searchPattern.Substring(0, slashIndex);
            resArr = Directory.GetDirectories(path, sp1);

            string res = null;
            foreach (var s in resArr)
            {
                try
                {
                    res = FindDirectory(System.IO.Path.Combine(path, s), searchPattern.Substring(sp1.Length + 1));
                    break;
                }
                catch (Exception)
                {
                }    
            }
            
            if (res == null)
                throw new Exception("Директория не найдена: " + System.IO.Path.Combine(path, searchPattern));

            return res;
        }
    }
}
