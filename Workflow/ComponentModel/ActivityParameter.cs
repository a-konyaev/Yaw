using System;
using System.Collections.Generic;
using System.Linq;
using Yaw.Core;
using Yaw.Core.Extensions;
using Yaw.Core.Utils.Collections;

namespace Yaw.Workflow.ComponentModel
{
    /// <summary>
    /// Параметр действия
    /// </summary>
    [Serializable]
    public class ActivityParameter : INamed
    {
        /// <summary>
        /// Имя параметра
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Вычислятель значения параметра действия
        /// </summary>
        public ActivityParameterEvaluator Evaluator
        {
            get;
            internal set;
        }

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        internal ActivityParameter()
        {
        }

        /// <summary>
        /// Конструктор с заданием имени параметра и простого значения
        /// </summary>
        /// <param name="name"></param>
        /// <param name="plainValue"></param>
        public ActivityParameter(string name, object plainValue)
        {
            CodeContract.Requires(!string.IsNullOrEmpty(name));
            CodeContract.Requires(plainValue != null);

            Name = name;
            Evaluator = new ActivityParameterEvaluator(plainValue);
        }

        /// <summary>
        /// Получить значение параметра
        /// </summary>
        public object GetValue()
        {
            if (Evaluator == null)
                return null;

            return Evaluator.GetValue();
        }

        /// <summary>
        /// Получить типизированное значение параметра
        /// </summary>
        public T GetValue<T>()
        {
            var value = GetValue();
            return CastValue<T>(value);
        }

        /// <summary>
        /// Получить значение параметра в виде массива
        /// </summary>
        /// <returns></returns>
        public object[] GetParamValueAsArray()
        {
            var value = GetValue();
            var type = value.GetType();

            return type.IsArray ? (object[])value : new[] { value };
        }

        /// <summary>
        /// Получить значение параметра в виде типизированного перечисления
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetParamValueAsEnumerable<T>()
        {
            var value = GetParamValueAsArray();
            return CastArrayValue<T>(value);
        }

        /// <summary>
        /// Получить значение параметра в виде массива элементов заданного типа
        /// </summary>
        /// <returns></returns>
        public T[] GetParamValueAsArray<T>()
        {
            var objectArray = GetParamValueAsArray();
            var resArray = new T[objectArray.Length];

            for (var i = 0; i < objectArray.Length; i++)
                resArray[i] = CastValue<T>(objectArray[i]);

            return resArray;
        }

        /// <summary>
        /// Приобразовывает значение-массив к перечислению с элементами заданного типа
        /// </summary>
        /// <typeparam name="T">тип элемента</typeparam>
        /// <param name="arrayValue">значение параметра (ранее вычисленное)</param>
        /// <returns></returns>
        private IEnumerable<T> CastArrayValue<T>(IEnumerable<object> arrayValue)
        {
            return arrayValue.Select(item => CastValue<T>(item));
        }

        /// <summary>
        /// Приобразовывает значение к заданному типу
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">значение параметра (ранее вычисленное)</param>
        /// <returns></returns>
        private T CastValue<T>(object value)
        {
            try
            {
                return (T)typeof(T).ConvertToType(value);
            }
            catch (Exception ex)
            {
                throw new InvalidCastException(string.Format(
                    "Ошибка приведения значения '{0}' к типу {1} при получении значения параметра {2}",
                    value, typeof(T).Name, Name), ex);
            }
        }
    }
}