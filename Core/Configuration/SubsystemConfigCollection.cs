﻿using System;
using System.Configuration;
using System.Xml;

namespace Yaw.Core.Configuration
{
    /// <summary>
    /// Коллекция конфигов подсистем
    /// </summary>
    public class SubsystemConfigCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// читатель конфиг-файла
        /// </summary>
        private XmlReader _reader;
        /// <summary>
        /// Наименование текущей подсистемы, конфиг которой обрабатывается
        /// </summary>
        private string _currentSubsystemName;


        protected override ConfigurationElement CreateNewElement()
        {
            // получим имя и название типа подсистемы из атрибутов
            string subsystemName = null;
            string subsystemTypeName = null;
            string traceLevelName = null;
            string logFileFolder = null;
            var separateLog = false;
            var disposeOrder = SubsystemConfig.DISPOSE_ORDER_UNDEFINED;

            for (var go = _reader.MoveToFirstAttribute(); go; go = _reader.MoveToNextAttribute())
            {
                switch (_reader.Name)
                {
                    case "name":
                        subsystemName = _reader.Value;
                        break;

                    case "type":
                        subsystemTypeName = _reader.Value;
                        break;

                    case "traceLevel":
                        traceLevelName = _reader.Value;
                        break;

                    case "logFileFolder":
                        logFileFolder = _reader.Value;
                        break;

                    case "separateLog":
                        separateLog = bool.Parse(_reader.Value);
                        break;

                    case "disposeOrder":
                        disposeOrder = int.Parse(_reader.Value);
                        break;

                    default:
                        throw new ConfigurationErrorsException("Неожиданный атрибут: " + _reader.Name, _reader);
                }
            }

            if (string.IsNullOrEmpty(subsystemTypeName))
                throw new ConfigurationErrorsException("Не задано имя типа класса подсистемы в атрибуте type", _reader);

            // получим тип подсистемы
            Type subsystemType;
            try
            {
                subsystemType = Type.GetType(subsystemTypeName, true);
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException("Ошибка получения типа подсистемы: " + subsystemTypeName,
                    ex, _reader);
            }

            // установим имя подсистемы
            if (string.IsNullOrEmpty(subsystemName))
                subsystemName = subsystemType.Name;

            // запомним имя текущей подсистемы
            _currentSubsystemName = subsystemName;

            // проверим, указан ли для этой подсистемы специфичный конфиг-элемент
            var atts = subsystemType.GetCustomAttributes(typeof(SubsystemConfigurationElementTypeAttribute), true);

            SubsystemConfig configElem;

            // если тип конфиг-элемента не задан
            if (atts.Length == 0)
            {
                // то используем базовый конфиг-элемент
                configElem = new SubsystemConfig();
            }
            else
            {
                // создадим конфиг-элемент из типа
                var att = (SubsystemConfigurationElementTypeAttribute)atts[0];
                
                try
                {
                    configElem = (SubsystemConfig)Activator.CreateInstance(att.Type);
                }
                catch (Exception ex)
                {
                    throw new ConfigurationErrorsException("Ошибка создания конфигурационного элемента из типа: "
                        + att.Type.FullName, ex, _reader);
                }
            }

            configElem.SubsystemName = subsystemName;
            configElem.SubsystemTypeName = subsystemTypeName;
            configElem.TraceLevelName = traceLevelName;
            configElem.LogFileFolder = logFileFolder;
            configElem.SeparateLog = separateLog;
            configElem.DisposeOrder = disposeOrder;

            return configElem;
        }

        protected override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
        {
            _reader = reader;
            try
            {
                base.DeserializeElement(_reader, serializeCollectionKey);
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException(
                    string.Format("Ошибка в конфигурации подсистемы '{0}': {1}", _currentSubsystemName, ex.Message),
                    ex,
                    reader);
            }
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SubsystemConfig)element).SubsystemName;
        }

        public new SubsystemConfig this[string name]
        {
            get
            {
                return (SubsystemConfig)BaseGet(name);
            }
        }
    }
}
