using System;
using System.Configuration;

namespace Yaw.Core.Configuration
{
    /// <summary>
    /// Валидатор значений типа ValueConfig<int>
    /// </summary>
    public class IntValueConfigValidator : ConfigurationValidatorBase
    {
        /// <summary>
        /// Минимальное значение
        /// </summary>
        public int MinValue { get; set; }
        /// <summary>
        /// Максимальное значение
        /// </summary>
        public int MaxValue { get; set; }

        public override bool CanValidate(Type type)
        {
            return type.Equals(typeof(ValueConfig<int>));
        }

        public override void Validate(object value)
        {
            var val = ((ValueConfig<int>)value).Value;

            if (MinValue > val || val > MaxValue)
                throw new ArgumentException(
                    string.Format("Значение должно быть в диапазоне [{0}, {1}]", MinValue, MaxValue));
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class IntValueConfigValidatorAttribute : ConfigurationValidatorAttribute
    {
        /// <summary>
        /// Минимальное значение
        /// </summary>
        public int MinValue { get; set; }
        /// <summary>
        /// Максимальное значение
        /// </summary>
        public int MaxValue { get; set; }

        public IntValueConfigValidatorAttribute()
        {
            MinValue = int.MinValue;
            MaxValue = int.MaxValue;
        }

        public override ConfigurationValidatorBase ValidatorInstance
        {
            get
            {
                return new IntValueConfigValidator
                           {
                               MinValue = MinValue,
                               MaxValue = MaxValue
                           };
            }
        }
    }
}
