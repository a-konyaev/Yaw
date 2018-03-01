using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using Yaw.Core;
using Yaw.Core.Diagnostics.Default;
using Yaw.Core.Extensions;
using Yaw.Core.Utils.Collections;
using Yaw.Workflow.Runtime;

namespace Yaw.Workflow.ComponentModel.Compiler
{
    /// <summary>
    /// Парсер схемы потока работ
    /// </summary>
    public class WorkflowSchemeParser
    {
        #region Константы
        private const string XML_TARGETNAMESPACE = "http://schemas.yaw.ru/Workflow";

        private const string ELEM_WORKFLOW = "Workflow";
        private const string ELEM_INCLUDE = "Include";
        private const string ELEM_COMPOSITEACTIVITY = "CompositeActivity";
        private const string ELEM_ACTIVITYPARAMETERSBINDINGS = "ActivityParametersBindings";
        private const string ELEM_ACTIVITYPARAMETERSBINDING = "ActivityParametersBinding";
        private const string ELEM_ACTIVITY = "Activity";
        private const string ELEM_REFERENCEACTIVITY = "ReferenceActivity";
        private const string ELEM_SUBSCRIBETOEVENT = "SubscribeToEvent";
        private const string ELEM_UNSUBSCRIBEFROMEVENT = "UnsubscribeFromEvent";
        private const string ELEM_MONITORENTER = "MonitorEnter";
        private const string ELEM_MONITOREXIT = "MonitorExit";
        private const string ELEM_PARAMETERS = "Parameters";
        private const string ELEM_NEXTACTIVITIES = "NextActivities";
        private const string ELEM_PARAMETER = "Parameter";
        private const string ELEM_NEXTACTIVITY = "NextActivity";
        private const string ELEM_REGION = "Region";

        private const string ATT_ROOTACTIVITY = "RootActivity";
        private const string ATT_DEFAULTNEXTACTIVITYKEY = "DefaultNextActivityKey";
        private const string ATT_ACTIVITYNAME = "ActivityName";
        private const string ATT_REF = "Ref";
        private const string ATT_NAME = "Name";
        private const string ATT_CLASS = "Class";
        private const string ATT_TRACKING = "Tracking";
        private const string ATT_INITIALIZE = "Initialize";
        private const string ATT_UNINITIALIZE = "Uninitialize";
        private const string ATT_EXECUTE = "Execute";
        private const string ATT_PARAMETERS = "Parameters";
        private const string ATT_NEXTACTIVITIES = "NextActivities";
        private const string ATT_DEFAULTNEXTACTIVITY = "DefaultNextActivity";
        private const string ATT_NEXTACTIVITY = "NextActivity";
        private const string ATT_KEY = "Key";
        private const string ATT_EVENT = "Event";
        private const string ATT_HANDLER = "Handler";
        private const string ATT_HANDLINGTYPE = "HandlingType";
        private const string ATT_LOCKNAME = "LockName";
        private const string ATT_PRIORITY = "Priority";
        private const string ATT_COMPOSITEACTIVITYNAME = "CompositeActivityName";

        private const char LIST_DELIMITER = ';';
        private const char VALUE_ASSIGN_CHAR = '=';

        private const char PREFIX_DELIMITER = '.';
        private const string PREFIX_ROOT = "Root";
        private const string PREFIX_REFTOROOT = "Root.";
        private const int PREFIX_REFTOROOTLEN = 5;

        internal const string PARAM_STARTACTIVITY = "StartActivity";
        #endregion

        #region Св-ва
        /// <summary>
        /// Список URI подключенных в схему файлов
        /// </summary>
        private readonly List<string> _includeFileUriList = new List<string>();

        /// <summary>
        /// Родительский парсер
        /// </summary>
        /// <remarks>Если схема, разбор которой выполняет парсер (т.е. наша схема), основная,
        /// то родительский парсер = null. Если же наша схема - это схема подключаемого файла,
        /// то родительский парсер != null и это парсер, который выполняет разбор основной схемы, 
        /// в которую подключают нашу схему</remarks>
        private WorkflowSchemeParser _parentParser;

        /// <summary>
        /// Признак того, что данный парсер главный, т.е. он разбирает основную схему
        /// </summary>
        private bool MainParser
        {
            get
            {
                return _parentParser == null;
            }
        }

        /// <summary>
        /// Читальщик xml-файла со схемой
        /// </summary>
        private XmlReaderEx _reader;

        /// <summary>
        /// Схема, которая получилась в результате разбора
        /// </summary>
        public WorkflowScheme Scheme
        {
            get;
            private set;
        }

        /// <summary>
        /// Таблица биндингов параметров для всех действий
        /// </summary>
        private readonly ByNameAccessDictionary<ParametersBindingActivity> _parametersBindings = 
            new ByNameAccessDictionary<ParametersBindingActivity>();

        /// <summary>
        /// Uri файла со схемой, разбор которого выполняет данный парсер
        /// </summary>
        private string _workflowSchemaUri;

        /// <summary>
        /// Настройки xml-ридера
        /// </summary>
        private XmlReaderSettings _xmlReaderSettings;

        /// <summary>
        /// Имя xml-файла, разбор которого выполняется
        /// </summary>
        public string FileName
        {
            get;
            private set;
        }

        /// <summary>
        /// Завершено ли чтение входного файла
        /// </summary>
        public bool ReadDone
        {
            get;
            private set;
        }

        /// <summary>
        /// Номер текущая строки в xml-файле, разбор которого выполняется
        /// </summary>
        public int LineNumber
        {
            get
            {
                return _reader.LineNumber;
            }
        }

        /// <summary>
        /// Номер текущей позиции в строке в xml-файле, разбор которого выполняется
        /// </summary>
        public int LinePosition
        {
            get
            {
                return _reader.LinePosition;
            }
        }
        #endregion

        /// <summary>
        /// Выполнить разбор входных данных и сформировать схему
        /// </summary>
        /// <param name="workflowSchemeUri">uri файла, разбор которого нужно выполнить</param>
        public void Parse(string workflowSchemeUri)
        {
            Parse(workflowSchemeUri, GetXmlReaderSettings(null));
        }

        /// <summary>
        /// Выполнить разбор входных данных и сформировать схему
        /// </summary>
        /// <param name="workflowSchemeUri">uri файла, разбор которого нужно выполнить</param>
        /// <param name="customXmlSchemas">информация о пользовательских xsd-схемах, которые нужно использовать
        /// при разборе. Это список пар {targetNamespace, XmlReader}</param>
        public void Parse(string workflowSchemeUri, IEnumerable<KeyValuePair<string, XmlReader>> customXmlSchemas)
        {
            Parse(workflowSchemeUri, GetXmlReaderSettings(customXmlSchemas));
        }

        /// <summary>
        /// Выполнить разбор входных данных и сформировать схему
        /// </summary>
        /// <param name="workflowSchemeUri">uri файла, разбор которого нужно выполнить</param>
        /// <param name="settings">настройки xml-ридера</param>
        private void Parse(string workflowSchemeUri, XmlReaderSettings settings)
        {
            CodeContract.Requires(!string.IsNullOrEmpty(workflowSchemeUri));

            _workflowSchemaUri = workflowSchemeUri;
            _xmlReaderSettings = settings;

            var fileInfo = new FileInfo(_workflowSchemaUri);
            FileName = fileInfo.Name;
            if (!fileInfo.Exists)
                throw new WorkflowSchemeParserException(string.Format("Файл {0} не найден", FileName));

            Scheme = new WorkflowScheme();
            ReadDone = false;

            // создаем xml-ридер
            using (_reader = XmlReaderEx.Create(_workflowSchemaUri, settings))
            {
                // читаем схему
                ReadScheme();
                ReadDone = true;

                // если это не главный парсер
                if (!MainParser)
                    // то проверки и пост-парсинговые вычисления не будем выполнять
                    return;

                // вычислим следующие действия, которые не смогли вычислить сразу же при чтении схемы
                EvaluateNextActivities();

                // проверка корректности задания след. действий
                ValidateNextActivities();

                // вычислим действия-обработчики событий, которые не смогли вычислить сразу же при чтении схемы
                EvaluateEventHandlerActivities();

                // вычислим действия, на которые ссылаются действия-ссылки, 
                // которые не смогли вычислить сразу же при чтении схемы
                EvaluateReferencedActivities();

                // выполняем связывание значений параметров
                BindParameters();

                // вычислим вычислители параметров, которые не смогли вычислить сразу же при чтении схемы 
                // sorry, за каламбур:)
                EvaluateParameterEvaluators();

                // вычислим держатели событий, которые не смогли вычислить сразу же при чтении схемы
                EvaluateEventHolders();

                // проверяем валидность имен параметров
                CheckParametersNames();

                // проверим корректность полученной схемы
                ValidateScheme();

                // добавим действие для выхода
                AddExitActivity();
            }
        }

        /// <summary>
        /// Возвращает настройки для xml-ридера
        /// </summary>
        /// <returns></returns>
        private static XmlReaderSettings GetXmlReaderSettings(
            IEnumerable<KeyValuePair<string, XmlReader>> customXmlSchemas)
        {
            var schemas = new XmlSchemaSet();

            // добавим главную схему
            var schemaXmlReader = XmlReader.Create(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("Yaw.Workflow.Workflow.xsd"));
            schemas.Add(XML_TARGETNAMESPACE, schemaXmlReader);

            // добавим пользовательские схемы
            if (customXmlSchemas != null)
                foreach (var item in customXmlSchemas)
                {
                    try
                    {
                        schemas.Add(item.Key, item.Value);
                    }
                    catch (Exception ex)
                    {
                        throw new WorkflowSchemeParserException(
                            "Ошибка добавления пользовательской xml-схемы: " + item.Key, ex);
                    }
                }

            var settings = new XmlReaderSettings
                               {
                                   ValidationType = ValidationType.Schema,
                                   Schemas = schemas,
                                   CloseInput = true,
                               };

            return settings;
        }

        /// <summary>
        /// Читает всю схему
        /// </summary>
        private void ReadScheme()
        {
            // берем корневой xml-элемент
            if (!_reader.MoveToFirstElement())
                return;

            if (_reader.Name != ELEM_WORKFLOW)
                throw new WorkflowSchemeParserException("Корневой элемент должен называться Workflow", this);

            // получим имя корневого действия
            var atts = ReadAttributes(new[] { ATT_ROOTACTIVITY, ATT_DEFAULTNEXTACTIVITYKEY });

            Scheme.RootActivityName = atts[ATT_ROOTACTIVITY];

            if (atts[ATT_DEFAULTNEXTACTIVITYKEY] != null)
                Scheme.DefaultNextActivityKey = new NextActivityKey(atts[ATT_DEFAULTNEXTACTIVITYKEY]);

            // если данный парсер главный
            if (MainParser)
            {
                if (string.IsNullOrEmpty(Scheme.RootActivityName))
                    throw new WorkflowSchemeParserException(
                        "Корневой элемент Workflow должен содержать имя корневого действия в атрибуте RootActivity", this);

                if (Scheme.DefaultNextActivityKey == null)
                    throw new WorkflowSchemeParserException(
                        "Корневой элемент Workflow должен содержать имя ключа следующего действия по умолчанию " +
                        "в атрибуте DefaultNextActivityKey", this);
            }
            // иначе, данный парсер разбирает подключаемую схему
            else
            {
                // пропишем имя корневого действия такое же, как у главного парсера
                Scheme.RootActivityName = _parentParser.Scheme.RootActivityName;

                // если в схеме не задан ключ следующего действия по умолчанию
                if (Scheme.DefaultNextActivityKey == null)
                    // то пропишем его такой же, как у основной схемы
                    Scheme.DefaultNextActivityKey = _parentParser.Scheme.DefaultNextActivityKey;

                // иначе, если ключ задан, но не совпадает с ключом основной схемы
                else if (!Scheme.DefaultNextActivityKey.Equals(_parentParser.Scheme.DefaultNextActivityKey))
                {
                    var rootParser = GetRootParser();

                    // то это ошибка
                    throw new WorkflowSchemeParserException(string.Format(
                        "Название ключа следующего действия по умолчанию '{0}' в подключаемом файле {1} " +
                        "должно совпадать с названием ключа следующего действия по умолчанию '{2}' в основном файле {3}",
                        Scheme.DefaultNextActivityKey.Name,
                        FileName,
                        rootParser.Scheme.DefaultNextActivityKey.Name,
                        rootParser.FileName), this);
                }
            }

            // перебираем вложенные элементы
            _reader.DownDepth();
            while (_reader.MoveToNextElementOnCurrentDepth())
            {
                switch (_reader.Name)
                {
                    case ELEM_INCLUDE:
                        ReadInclude();
                        break;

                    case ELEM_COMPOSITEACTIVITY:
                        ReadCompositeActivity();
                        break;

                    case ELEM_ACTIVITYPARAMETERSBINDINGS:
                        ReadActivityParametersBindings();
                        break;

                    default:
                        throw new WorkflowSchemeParserException(
                            string.Format("Неожиданный элемент: {0}", _reader.Name), this);
                }
            }
            _reader.UpDepth();
        }

        /// <summary>
        /// Добавляет действие в схему и проверяет, что в схеме еще нет действия с таким же именем
        /// </summary>
        /// <param name="activity"></param>
        private void AddActivityToScheme(Activity activity)
        {
            if (Scheme.Activities.ContainsKey(activity.Name))
                throw new WorkflowSchemeParserException(string.Format(
                    "Действие с именем {0} уже объявлено ранее", activity.Name), this);

            Scheme.Activities.Add(activity);
        }

        /// <summary>
        /// Получить корневой парсер, т.е. парсер который разбирает самый верхний в иерархии файл
        /// </summary>
        /// <returns></returns>
        private WorkflowSchemeParser GetRootParser()
        {
            var parser = this;

            while (parser._parentParser != null)
                parser = parser._parentParser;

            return parser;
        }

        #region Общие методы парсинга

        /// <summary>
        /// Читает заданные атрибуты
        /// </summary>
        /// <remarks>если встретится атрибут, кот. нет в списке заданных, то сгенерится исключение</remarks>
        /// <param name="requiredAttNames">имена заданных атрибутов</param>
        /// <returns>словарь: имя заданного атрибута -> значение атрибута. 
        /// Если заданного атрибута не было, то его значение будет = null</returns>
        private StringDictionary ReadAttributes(string[] requiredAttNames)
        {
            NameValueCollection otherAttributes;
            var res = ReadAllAttributes(requiredAttNames, out otherAttributes);

            if (otherAttributes.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var name in otherAttributes.Keys)
                {
                    sb.Append(name);
                    sb.Append(',');
                }
                sb.Length -= 1;

                throw new WorkflowSchemeParserException(string.Format("Неожиданные атрибуты: {0}", sb), this);
            }

            return res;
        }

        /// <summary>
        /// Читаем заданные атрибуты, а также возвращает список атрибутов, которых нет среди заданных
        /// </summary>
        /// <param name="requiredAttNames">имена заданных атрибутов</param>
        /// <param name="otherAttributes">список незаданных атрибутов, которые были прочитаны</param>
        /// <returns>словарь: имя заданного атрибута -> значение атрибута. 
        /// Если заданного атрибута не было, то его значение будет = null</returns>
        private StringDictionary ReadAllAttributes(string[] requiredAttNames, out NameValueCollection otherAttributes)
        {
            otherAttributes = new NameValueCollection();

            var atts = new StringDictionary();
            foreach (var attName in requiredAttNames)
                atts.Add(attName, null);

            while (_reader.MoveToNextAttribute())
            {
                var attName = _reader.Name;
                if (attName.StartsWith("xmlns"))
                    continue;

                if (_reader.Prefix.Length > 0)
                    attName = _reader.Name.Substring(_reader.Prefix.Length + 1);

                var attValue = _reader.Value;
                if (atts.ContainsKey(attName))
                {
                    if (!string.IsNullOrEmpty(attValue))
                        atts[attName] = attValue;
                }
                else
                    otherAttributes.Add(attName, attValue);
            }

            return atts;
        }

        /// <summary>
        /// Читает содержимое текущего элемента
        /// </summary>
        /// <returns></returns>
        private string ReadElementContent()
        {
            try
            {
                _reader.MoveToContent();
                return _reader.ReadElementString();
            }
            catch (Exception ex)
            {
                throw new WorkflowSchemeParserException("Ошибка чтения содержимого для элемента", ex, this);
            }
        }

        /// <summary>
        /// Делегат для обработки пары имя+значение
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        private delegate void ProcessNameValue(string name, string value);

        /// <summary>
        /// Читает строку типа "имя=значение;...;имя=значение" и 
        /// вызывает для каждой пары имя+значение заданный метод-обработчик
        /// </summary>
        /// <param name="nameAndValueString"></param>
        /// <param name="processMethod"></param>
        private void ReadNameAndValueString(string nameAndValueString, ProcessNameValue processMethod)
        {
            var nameAndValueList = nameAndValueString.Split(LIST_DELIMITER);
            foreach (var nameAndValue in nameAndValueList)
            {
                var index = nameAndValue.IndexOf(VALUE_ASSIGN_CHAR);
                if (index == -1)
                    throw new WorkflowSchemeParserException(string.Format(
                        "Неправильный формат строки: {0}", nameAndValueString), this);

                var name = nameAndValue.Substring(0, index);
                if (string.IsNullOrEmpty(name))
                    throw new WorkflowSchemeParserException(string.Format(
                        "Неправильный формат строки. Не задано имя: {0}", nameAndValue), this);

                var value = nameAndValue.Substring(index + 1);
                if (string.IsNullOrEmpty(value))
                    throw new WorkflowSchemeParserException(string.Format(
                        "Неправильный формат строки. Не задано значение: {0}", nameAndValue), this);

                processMethod(name, value);
            }
        }

        #endregion

        #region Биндинг параметров

        /// <summary>
        /// Читаем элемент ActivityParametersBindings
        /// </summary>
        private void ReadActivityParametersBindings()
        {
            var atts = ReadAttributes(new[] { ATT_COMPOSITEACTIVITYNAME });
            var compositeActivityName = atts[ATT_COMPOSITEACTIVITYNAME];

            if (compositeActivityName == null)
                throw new WorkflowSchemeParserException(
                    "Не задано имя составного действия для связывания значений параметров", this);

            // перебираем вложенные элементы
            _reader.DownDepth();
            while (_reader.MoveToNextElementOnCurrentDepth())
            {
                switch (_reader.Name)
                {
                    case ELEM_ACTIVITYPARAMETERSBINDING:
                        ReadActivityParametersBinding(compositeActivityName);
                        break;

                    default:
                        throw new WorkflowSchemeParserException(
                            string.Format("Неожиданный элемент: {0}", _reader.Name), this);
                }
            }
            _reader.UpDepth();
        }

        /// <summary>
        /// Читаем элемент ActivityParametersBinding
        /// </summary>
        /// <param name="compositeActivityName">имя составного действия</param>
        private void ReadActivityParametersBinding(string compositeActivityName)
        {
            var atts = ReadAttributes(new[] { ATT_ACTIVITYNAME, ATT_PARAMETERS });
            var activityName = atts[ATT_ACTIVITYNAME];
            var parametersAttValue = atts[ATT_PARAMETERS];

            if (activityName == null)
                throw new WorkflowSchemeParserException(
                    "Не задано имя действия для связывания значений параметров", this);

            // получим полное имя действия:
            // - если локальное имя = '.', значит это биндинг параметров для самого составного действия
            // - иначе, сформируем полное имя действия
            activityName = string.CompareOrdinal(activityName, ".") == 0
                ? compositeActivityName : CreateFullActivityName(activityName, compositeActivityName);

            ParametersBindingActivity paramsBinding;

            // попробуем найти описание биндинга для данного действия
            if (_parametersBindings.ContainsKey(activityName))
            {
                paramsBinding = _parametersBindings[activityName];
            }
            else
            {
                paramsBinding = new ParametersBindingActivity {Name = activityName};
                _parametersBindings.Add(paramsBinding);
            }

            // если параметры заданы в атрибуты
            if (parametersAttValue != null)
                ReadActivityParametersFromAttValue(paramsBinding, parametersAttValue);

            // читаем параметры из вложенных элементов
            ReadActivityParameters(paramsBinding);
        }

        /// <summary>
        /// выполняем связывание значений параметров
        /// </summary>
        private void BindParameters()
        {
            foreach (var paramsBinding in _parametersBindings.Values)
            {
                if (!Scheme.Activities.ContainsKey(paramsBinding.Name))
                    // по идее, этого быть не должно, т.к. имена действий в биндинге проверяли при формировании
                    // таблицы биндингов
                    throw new WorkflowSchemeParserException(string.Format(
                        "Ошибка связывания значений параметров. Действие с именем {0} не найдено",
                        paramsBinding.Name), this);

                var activity = Scheme.Activities[paramsBinding.Name];

                // ориентируясь по именам параметров, добавим в действие параметры, которых у него еще нет
                foreach (var param in paramsBinding.Parameters)
                {
                    if (!activity.Parameters.Contains(param))
                        activity.Parameters.Add(param);
                }
            }
        }

        #endregion

        #region Подключение внешних схем

        /// <summary>
        /// Читаем элемент Include
        /// </summary>
        private void ReadInclude()
        {
            var refUri = ReadAttributes(new[] { ATT_REF })[ATT_REF];

            if (refUri == null)
                throw new WorkflowSchemeParserException("Не задан Uri подключаемого файла с описанием схемы", this);

            // если данный файл уже подключали ранее
            if (IsFileAlreadyIncluded(refUri, this))
                // то ничего не делаем
                return;

            // запустим разбор подключаемого файла
            var parser = new WorkflowSchemeParser {_parentParser = this};
            parser.Parse(refUri, _xmlReaderSettings);

            // добавим все действия из другой схемы в нашу
            foreach (var activity in parser.Scheme.Activities.Values)
            {
                AddActivityToScheme(activity);
            }

            // добавим таблицу биндингов
            foreach (var paramsBinding in parser._parametersBindings.Values)
            {
                // если биндинг параметров с таким именем (т.е. для действия с таким именем) уже есть
                if (_parametersBindings.ContainsKey(paramsBinding.Name))
                {
                    // то добавим биндинг параметров, для которых еще нет биндинга
                    var existingParamsBinding = _parametersBindings[paramsBinding.Name];

                    foreach (var param in paramsBinding.Parameters)
                    {
                        if (existingParamsBinding.Parameters.Contains(param))
                            throw new WorkflowSchemeParserException(string.Format(
                                "В файле {0} (или в подключенных в него файлах) содержится " + 
                                "связывание значения параметра {1} для действия {2}, " + 
                                "которое уже объявлено ранее", 
                                parser.FileName, param.Name, paramsBinding.Name), this);

                        // добавим биндинг параметра
                        existingParamsBinding.Parameters.Add(param);
                    }
                }
                else
                {
                    _parametersBindings.Add(paramsBinding);
                }
            }

            // добавим данный файл в список уже подключенных
            _includeFileUriList.Add(refUri);

            // а также добавим в список уже подключенных те файлы, которые подключены в подключаемую схему
            foreach (var fileUri in parser._includeFileUriList)
            {
                _includeFileUriList.Add(fileUri);
            }
        }

        /// <summary>
        /// Подключен ли уже заданный файл
        /// </summary>
        /// <remarks>проверяет, что заданный файл уже подключен или в данную схему, 
        /// или в родительскую схему, если данная схема также подключается.
        /// Если Uri файла совпадает Uri файла данной схемы, то это ошибка (циклическая ссылка)</remarks>
        /// <param name="fileUri"></param>
        /// <param name="startSearchIncludesParser">парсер, который начал выполнять проверку того, что файл подключен</param>
        /// <returns></returns>
        private bool IsFileAlreadyIncluded(string fileUri, WorkflowSchemeParser startSearchIncludesParser)
        {
            if (_workflowSchemaUri.Equals(fileUri))
                throw new WorkflowSchemeParserException(string.Format(
                    "Обнаружена циклическая ссылка при подключении файла {0}", fileUri), startSearchIncludesParser);

            return _includeFileUriList.Contains(fileUri) ||
                (_parentParser != null && _parentParser.IsFileAlreadyIncluded(fileUri, startSearchIncludesParser));
        }

        #endregion

        #region Составное действие

        /// <summary>
        /// Читает содержимое составного действия, которое является текущим узлом XmlReader-а
        /// </summary>
        private void ReadCompositeActivity()
        {
            // создаем действие
            var activity = CreateCompositeActivity();
            // добавляем действие в схему
            AddActivityToScheme(activity);

            // последнее прочитанное действие
            Activity lastActivity = null;
            // погружатель в регион
            var regionDiver = new RegionDiver(_reader);

            // перебираем вложенные элементы
            _reader.DownDepth();
            while (true)
            {
                // пробуем перейти к след. элементу
                var moveDone = _reader.MoveToNextElementOnCurrentDepth();

                // если не удалось перейти к след. элементу на текущем уровне
                if (!moveDone)
                {
                    // то пробуем выйти на уровень выше (это если мы внутри региона) 
                    // и еще раз перейти к след. элементу
                    while (regionDiver.ExitFromRegion())
                    {
                        moveDone = _reader.MoveToNextElementOnCurrentDepth();
                        if (moveDone)
                            break;
                    }
                }

                // если так и не удалось перейти к след. элементу
                if (!moveDone)
                    break;

                Activity currentActivity;
                switch (_reader.Name)
                {
                    case ELEM_ACTIVITY:
                        currentActivity = ReadActivity(activity);
                        break;

                    case ELEM_REFERENCEACTIVITY:
                        currentActivity = ReadReferenceActivity(activity);
                        break;

                    case ELEM_SUBSCRIBETOEVENT:
                        currentActivity = ReadSubscribeToEventActivity(activity);
                        break;

                    case ELEM_UNSUBSCRIBEFROMEVENT:
                        currentActivity = ReadUnsubscribeFromEventActivity(activity);
                        break;

                    case ELEM_MONITORENTER:
                        currentActivity = ReadMonitorEnterActivity(activity);
                        break;

                    case ELEM_MONITOREXIT:
                        currentActivity = ReadMonitorExitActivity(activity);
                        break;

                    case ELEM_REGION:
                        // входим в регион
                        regionDiver.EnterToRegion();
                        continue;

                    default:
                        throw new WorkflowSchemeParserException(
                            string.Format("Неожиданный элемент: {0}", _reader.Name), this);
                }

                if (currentActivity != null)
                {
                    if (lastActivity != null)
                        // пропишем ссылку на действие, идущее следом
                        lastActivity.FollowingActivity = currentActivity;

                    lastActivity = currentActivity;
                }
            }
            regionDiver.ExitFromAllRegions();
            _reader.UpDepth();

            // проверим, что было добавлено хотя бы одно дочернее действие
            if (activity.Activities.Count == 0)
                throw new WorkflowSchemeParserException(string.Format(
                    "Составное действие {0} должно содержать хотя бы одно дочернее действие", activity.Name), this);
        }

        /// <summary>
        /// Возвращает значение атрибута Tracking из коллекции значений атрибутов
        /// </summary>
        /// <param name="atts"></param>
        /// <returns></returns>
        private bool GetTrackingAttValue(StringDictionary atts)
        {
            var trackingStr = atts[ATT_TRACKING];

            if (trackingStr == null)
                return true;

            bool tracking;
            if (!bool.TryParse(trackingStr, out tracking))
                throw new WorkflowSchemeParserException("Некорректно задано значение атрибута Tracking", this);

            return tracking;
        }

        /// <summary>
        /// Создает составное действие
        /// </summary>
        /// <returns></returns>
        private CompositeActivity CreateCompositeActivity()
        {
            CompositeActivity activity;

            var atts = ReadAttributes(new[] { ATT_NAME, ATT_CLASS, ATT_TRACKING });
            var activityName = atts[ATT_NAME];
            var activityClassTypeName = atts[ATT_CLASS];
            var tracking = GetTrackingAttValue(atts);

            if (activityName == null)
                throw new WorkflowSchemeParserException("Не задано имя составного действия", this);

            // если задан тип класса
            if (activityClassTypeName != null)
            {
                // создадим тип действия
                Type activityType;
                try
                {
                    activityType = Type.GetType(activityClassTypeName, true);
                }
                catch (Exception ex)
                {
                    throw new WorkflowSchemeParserException(string.Format(
                        "Ошибка загрузки типа класса действия. Имя типа класса = {0}", activityClassTypeName),
                        ex, this);
                }

                if (activityType == null)
                    throw new WorkflowSchemeParserException(string.Format(
                        "Тип класса действия с именем {0} не найден", activityClassTypeName), this);

                // проверим, что он унаследован от CompositeActivity
                if (!activityType.IsInheritedFromType(typeof(CompositeActivity)))
                    throw new WorkflowSchemeParserException(string.Format(
                        "Класс '{0}' должен быть унаследован от CompositeActivity", activityClassTypeName), this);

                // проверим, что у класса есть конструктор без параметров
                if (activityType.GetConstructor(new Type[] { }) == null)
                    throw new WorkflowSchemeParserException(string.Format(
                        "Для класса '{0}' не найден конструктор", activityClassTypeName), this);

                // создаем действие 
                try
                {
                    activity = (CompositeActivity)Activator.CreateInstance(activityType);
                }
                catch (Exception ex)
                {
                    throw new WorkflowSchemeParserException(string.Format(
                        "Ошибка создания экземпляра класса действия. Имя типа класса = {0}", activityClassTypeName),
                        ex, this);
                }
            }
            else
            {
                // иначе, создаем базовое составное действие
                activity = new CompositeActivity();
            }

            // пропишем имя действия
            activity.Name = activityName;
            // пропишем включенность режима отслеживания
            activity.Tracking = tracking;

            return activity;
        }

        /// <summary>
        /// Читает описание действия
        /// </summary>
        /// <param name="parentActivity"></param>
        private Activity ReadActivity(CompositeActivity parentActivity)
        {
            // создадим действие
            var activity = new Activity();

            // прочитаем общие атрибуты
            string activityExecutionMethod;
            ReadActivityCommon(activity, parentActivity, out activityExecutionMethod);

            // пропишем метод для выполнения действия
            activity.ExecutionMethodCaller = GetActivityExecutionMethodCaller(
                activityExecutionMethod, parentActivity);

            return activity;
        }

        /// <summary>
        /// Читаем общие атрибуты действия
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="parentActivity"></param>
        /// <param name="activityExecutionMethod"></param>
        private void ReadActivityCommon(
            Activity activity, CompositeActivity parentActivity, out string activityExecutionMethod)
        {
            // читаем атрибуты, при этом все атрибуты, которые не относятся к заданным,
            // считаем, что это атрибуты задающие след. действия
            NameValueCollection nextActivitiesAtts;
            var atts = ReadAllAttributes(
                new[] { 
                    ATT_NAME, 
                    ATT_INITIALIZE, 
                    ATT_UNINITIALIZE, 
                    ATT_EXECUTE, 
                    ATT_PRIORITY, 
                    ATT_PARAMETERS, 
                    ATT_NEXTACTIVITIES, 
                    ATT_DEFAULTNEXTACTIVITY,
                    ATT_TRACKING },
                out nextActivitiesAtts);

            var activityName = atts[ATT_NAME];
            var activityInitializeMethod = atts[ATT_INITIALIZE];
            var activityUninitializeMethod = atts[ATT_UNINITIALIZE];
            activityExecutionMethod = atts[ATT_EXECUTE];
            var parametersAttValue = atts[ATT_PARAMETERS];
            var nextActivitiesAttValue = atts[ATT_NEXTACTIVITIES];
            var defaultNextActivityAttValue = atts[ATT_DEFAULTNEXTACTIVITY];
            var tracking = GetTrackingAttValue(atts);

            // проверим, что все необходимые атрибуты заданы
            if (activityName == null)
                throw new WorkflowSchemeParserException("Не задано имя действия", this);

            if (activityExecutionMethod == null)
                throw new WorkflowSchemeParserException("Не задан метод, реализующий логику действия", this);

            // пропишем имя
            activity.Name = CreateFullActivityName(activityName, parentActivity);

            // получим методы инициализации и деинициализации
            if (activityInitializeMethod != null)
                activity.InitializeMethodCaller = 
                    GetActivityUnInitializeMethodCaller(activityInitializeMethod, parentActivity);
            if (activityUninitializeMethod != null)
                activity.UninitializeMethodCaller =
                    GetActivityUnInitializeMethodCaller(activityUninitializeMethod, parentActivity);

            // установим приоритет
            SetPriority(activity, atts[ATT_PRIORITY]);
            // пропишем включенность режима отслеживания
            activity.Tracking = tracking;
            // добавим действие в схему
            AddActivityToScheme(activity);

            // добавим действие в список дочерних составного действия
            parentActivity.Activities.Add(activity);
            activity.Parent = parentActivity;

            // если параметры заданы в атрибуте Parameters
            if (parametersAttValue != null)
                ReadActivityParametersFromAttValue(activity, parametersAttValue);

            // если след. действия заданы в атрибуте NextActivities
            if (nextActivitiesAttValue != null)
            {
                ReadNameAndValueString(
                    nextActivitiesAttValue,
                    (name, value) => AddNextActivity(activity, name, value));
            }

            // переберем след. действия, кот. заданы в произвольных атрибутах
            for (int i = 0; i < nextActivitiesAtts.Count; i++)
            {
                var attName = nextActivitiesAtts.GetKey(i);
                var attValue = nextActivitiesAtts.Get(i);
                AddNextActivity(activity, attName, attValue);
            }

            // если задано след. действие по умолчанию
            if (defaultNextActivityAttValue != null)
            {
                AddNextActivity(activity, NextActivityKey.DefaultNextActivityKey, defaultNextActivityAttValue);
            }

            // читаем вложенные элементы
            ReadActivityInnerElements(activity);
        }

        /// <summary>
        /// Задает значение приоритета действия из строкового значения атрибута
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="priorityAttValue"></param>
        /// <returns></returns>
        private void SetPriority(Activity activity, string priorityAttValue)
        {
            try
            {
                if (priorityAttValue == null)
                    return;

                activity.Priority = new ActivityPriority(int.Parse(priorityAttValue));
            }
            catch (Exception ex)
            {
                throw new WorkflowSchemeParserException("Некорректный приоритет: " + priorityAttValue, ex, this);
            }
        }

        /// <summary>
        /// Читает вложенные элементы действия типа Activity или ReferenceActivity
        /// </summary>
        /// <param name="activity"></param>
        private void ReadActivityInnerElements(Activity activity)
        {
            _reader.DownDepth();
            while (_reader.MoveToNextElementOnCurrentDepth())
            {
                switch (_reader.Name)
                {
                    case ELEM_PARAMETERS:
                        ReadActivityParameters(activity);
                        break;

                    case ELEM_NEXTACTIVITIES:
                        ReadNextActivities(activity);
                        break;

                    default:
                        throw new WorkflowSchemeParserException(
                            string.Format("Неожиданный элемент: {0}", _reader.Name), this);
                }
            }
            _reader.UpDepth();
        }

        /// <summary>
        /// Формирует полное имя для действия
        /// </summary>
        /// <param name="localActivityName"></param>
        /// <param name="parentActivity"></param>
        /// <returns></returns>
        internal static string CreateFullActivityName(string localActivityName, Activity parentActivity)
        {
            CodeContract.Requires(parentActivity != null);

            return CreateFullActivityName(localActivityName, parentActivity.Name);
        }

        /// <summary>
        /// Формирует полное имя для действия
        /// </summary>
        /// <param name="localActivityName"></param>
        /// <param name="parentActivityName"></param>
        /// <returns></returns>
        internal static string CreateFullActivityName(string localActivityName, string parentActivityName)
        {
            CodeContract.Requires(!string.IsNullOrEmpty(localActivityName));
            CodeContract.Requires(!string.IsNullOrEmpty(parentActivityName));

            return string.Format("{0}.{1}", parentActivityName, localActivityName);
        }

        /// <summary>
        /// Возвращает вызыватель метода выполнения действия по названию метода
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="compositeActivity"></param>
        /// <returns></returns>
        private ActivityExecutionMethodCaller GetActivityExecutionMethodCaller(
            string methodName, CompositeActivity compositeActivity)
        {
            // если имя метода имеет префикс Root.
            if (methodName.StartsWith(PREFIX_REFTOROOT))
            {
                // значит метод берем из корневого действия
                methodName = methodName.Substring(PREFIX_REFTOROOTLEN);
                return new ActivityExecutionMethodCaller(methodName, Scheme.RootActivity);
            }

            // иначе метод берем из текущего составного действия
            return new ActivityExecutionMethodCaller(methodName, compositeActivity);
        }

        /// <summary>
        /// Возвращает вызыватель метода инициализации/деинициализации действия по названию метода
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="compositeActivity"></param>
        /// <returns></returns>
        private ActivityUnInitializeMethodCaller GetActivityUnInitializeMethodCaller(
            string methodName, CompositeActivity compositeActivity)
        {
            // если имя метода имеет префикс Root.
            if (methodName.StartsWith(PREFIX_REFTOROOT))
            {
                // значит метод берем из корневого действия
                methodName = methodName.Substring(PREFIX_REFTOROOTLEN);
                return new ActivityUnInitializeMethodCaller(methodName, Scheme.RootActivity);
            }
            
            // иначе метод берем из текущего составного действия
            return new ActivityUnInitializeMethodCaller(methodName, compositeActivity);
        }

        #region Параметры действия

        /// <summary>
        /// Читает параметры действия
        /// </summary>
        /// <param name="activity"></param>
        private void ReadActivityParameters(Activity activity)
        {
            // читаем вложенные элементы
            _reader.DownDepth();
            while (_reader.MoveToNextElementOnCurrentDepth())
            {
                switch (_reader.Name)
                {
                    case ELEM_PARAMETER:
                        ReadActivityParameter(activity);
                        break;

                    default:
                        throw new WorkflowSchemeParserException(
                            string.Format("Неожиданный элемент: {0}", _reader.Name), this);
                }
            }
            _reader.UpDepth();
        }

        /// <summary>
        /// Читает параметры действия из значения атрибута Parameters
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="parametersAttValue"></param>
        private void ReadActivityParametersFromAttValue(Activity activity, string parametersAttValue)
        {
            ReadNameAndValueString(
                parametersAttValue,
                (name, value) =>
                    {
                        // создаем параметр
                        var param = new ActivityParameter {Name = name.Trim()};

                        // проверим, что параметр с таким именем еще не добавляли
                        if (activity.Parameters.Contains(param))
                            throw new WorkflowSchemeParserException(string.Format(
                                "Параметр с именем {0} уже добавлен ранее", param.Name), this);

                        // добавим параметр к действию
                        activity.Parameters.Add(param);

                        // читаем значение параметра
                        ReadParameterValue(activity, param, value);
                    });
        }

        /// <summary>
        /// Читает параметр действия из элемента Parameter, на кот. в данный момент указывает _reader
        /// </summary>
        /// <param name="activity"></param>
        private void ReadActivityParameter(Activity activity)
        {
            var param = new ActivityParameter
                            {
                                Name = ReadAttributes(new[] {ATT_NAME})[ATT_NAME]
                            };

            // проверим, что все необходимые атрибуты заданы
            if (param.Name == null)
                throw new WorkflowSchemeParserException("Не задано имя параметра действия", this);

            // проверим, что параметр с таким именем еще не добавляли
            if (activity.Parameters.Contains(param))
                throw new WorkflowSchemeParserException(string.Format(
                    "Параметр с именем {0} уже добавлен ранее", param.Name), this);

            // добавим параметр к действию
            activity.Parameters.Add(param);

            // читаем значение параметра действия
            var paramValue = ReadElementContent();
            if (string.IsNullOrEmpty(paramValue))
                throw new WorkflowSchemeParserException(string.Format(
                    "Не задано значение параметра {0}", param.Name), this);

            ReadParameterValue(activity, param, paramValue);
        }

        /// <summary>
        /// Читает значение параметра действия
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="param"></param>
        /// <param name="paramValue"></param>
        private void ReadParameterValue(Activity activity, ActivityParameter param, string paramValue)
        {
            param.Evaluator = GetParameterEvaluator(activity, param, paramValue);
        }

        /// <summary>
        /// Возвращает вычислитель для значения параметра
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="param"></param>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        private ActivityParameterEvaluator GetParameterEvaluator(
            Activity activity, ActivityParameter param, string paramValue)
        {
            // если значение параметра - это ссылка
            if (paramValue.StartsWith("@"))
            {
                paramValue = paramValue.Substring(1);

                if (string.IsNullOrEmpty(paramValue))
                    // т.е. значение = "@"
                    throw new WorkflowSchemeParserException(string.Format(
                        "Некорректное значение параметра {0}: {1}", param.Name, paramValue), this);

                // если значение параметра - это ссылка на встроенную функцию
                if (paramValue.StartsWith("@"))
                {
                    paramValue = paramValue.Substring(1);

                    if (string.IsNullOrEmpty(paramValue))
                        // т.е. значение = "@@"
                        throw new WorkflowSchemeParserException(string.Format(
                            "Некорректное значение параметра {0}: {1}", param.Name, paramValue), this);

                    try
                    {
                        return WorkflowBuiltinFunctions.GetEvaluatorForBuiltinFunction(paramValue);
                    }
                    catch (Exception ex)
                    {
                        throw new WorkflowSchemeParserException(
                            "Ошибка получения метода для получения значения параметра", ex, this);
                    }
                }

                // иначе значение параметра - это ссылка на св-во составного действия
                int dotIndex = paramValue.IndexOf(PREFIX_DELIMITER);

                // если ссылка содержит префикс, который записывается вначале через точку
                if (dotIndex > 0)
                {
                    var strArr = paramValue.Split(PREFIX_DELIMITER);
                    var prefix = strArr[0];
                    var propName = strArr[1];

                    if (string.IsNullOrEmpty(propName))
                        // т.е. значение = "@Префикс."
                        throw new WorkflowSchemeParserException(string.Format(
                            "Некорректное значение параметра {0}: {1}", param.Name, paramValue), this);

                    // определим по префиксу действие, на кот. ссылаются

                    // если это ссылка на св-во корневого составного действия
                    if (prefix == PREFIX_ROOT)
                        prefix = Scheme.RootActivityName;

                    // найдем действие по имени
                    var propOwner = GetReferencedActivity(prefix);

                    return GetParameterEvaluatorForPropertyReference(propName, propOwner);
                }
                
                // иначе - это ссылка на св-во родительского составного действия
                if (activity.Parent == null)
                    throw new WorkflowSchemeParserException(string.Format(
                        "Для значения-ссылки @{0} параметра {1} не определено родительское действие",
                        paramValue, param.Name), this);
 
                return GetParameterEvaluatorForPropertyReference(paramValue, activity.Parent);
            }
            
            // иначе, если значение параметра - это массив
            if (paramValue.StartsWith("[") && paramValue.EndsWith("]"))
            {
                paramValue = paramValue.Substring(1, paramValue.Length - 2);
                var array = paramValue.Split(',');

                // для каждого элемента массива нужно вычислить вычислитель значения
                var evaluatorArray = new ActivityParameterEvaluator[array.Length];

                for (int i = 0; i < array.Length; i++)
                    evaluatorArray[i] = GetParameterEvaluator(activity, param, array[i]);

                return new ActivityParameterEvaluator(evaluatorArray);
            }
            
            // иначе, это просто значение в чистом виде
            if (paramValue.StartsWith(@"\@"))
                paramValue = '@' + paramValue.Substring(2);

            return new ActivityParameterEvaluator(paramValue);
        }

        /// <summary>
        /// Получить вычислитель значения параметра действия для значения, 
        /// которое является ссылкой на св-во другого действия
        /// </summary>
        /// <param name="propertyName">имя св-ва</param>
        /// <param name="propertyOwner">действие, на св-во которого ссылаются</param>
        private ActivityParameterEvaluator GetParameterEvaluatorForPropertyReference(
            string propertyName, Activity propertyOwner)
        {
            var unevalRefActivity = propertyOwner as UnevaluatedActivity;
            return unevalRefActivity != null
                       ? new UnevaluatedActivityParameterEvaluator(propertyName, unevalRefActivity)
                       : CreateActivityParameterEvaluator(propertyName, propertyOwner);
        }

        /// <summary>
        /// Создает вычислитель значения параметра, который для вычисления обращается к св-ву
        /// </summary>
        /// <param name="propertyName">имя св-ва</param>
        /// <param name="propertyOwner">владелец св-ва</param>
        /// <returns></returns>
        private ActivityParameterEvaluator CreateActivityParameterEvaluator(string propertyName, Activity propertyOwner)
        {
            // найдем св-во в типе объекта propertyOwner
            var type = propertyOwner.GetType();
            PropertyInfo propInfo;
            try
            {
                propInfo = type.GetProperty(propertyName, true, false);
            }
            catch (Exception ex)
            {
                throw new WorkflowSchemeParserException(string.Format(
                    "Ошибка получения св-ва {0}", propertyName), ex, this);
            }

            return new ActivityParameterEvaluator(propInfo, propertyOwner);
        }

        #endregion

        #region Следующие действия

        /// <summary>
        /// Читает список следующих действий
        /// </summary>
        /// <param name="activity"></param>
        private void ReadNextActivities(Activity activity)
        {
            // читаем вложенные элементы
            _reader.DownDepth();
            while (_reader.MoveToNextElementOnCurrentDepth())
            {
                switch (_reader.Name)
                {
                    case ELEM_NEXTACTIVITY:
                        ReadNextActivity(activity);
                        break;

                    default:
                        throw new WorkflowSchemeParserException(
                            string.Format("Неожиданный элемент: {0}", _reader.Name), this);
                }
            }
            _reader.UpDepth();
        }

        /// <summary>
        /// Читает описание следующего действия
        /// </summary>
        /// <param name="activity"></param>
        private void ReadNextActivity(Activity activity)
        {
            var nextActivityKeyName = ReadAttributes(new[] { ATT_KEY })[ATT_KEY];
            var nextActivityKeyValue = ReadElementContent();

            AddNextActivity(activity, nextActivityKeyName, nextActivityKeyValue);
        }

        /// <summary>
        /// Добавляет в действие информацию о следующем действии
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="nextActivityKeyName"></param>
        /// <param name="nextActivityKeyValue"></param>
        private void AddNextActivity(Activity activity, string nextActivityKeyName, string nextActivityKeyValue)
        {
            // проверим, что все необходимые атрибуты заданы
            if (nextActivityKeyName == null)
                throw new WorkflowSchemeParserException("Не задан ключ следующего действия", this);

            var nextActivityKey = new NextActivityKey(nextActivityKeyName);

            AddNextActivity(activity, nextActivityKey, nextActivityKeyValue);
        }

        /// <summary>
        /// Добавляет в действие информацию о следующем действии
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="nextActivityKey"></param>
        /// <param name="nextActivityKeyValue"></param>
        private void AddNextActivity(Activity activity, NextActivityKey nextActivityKey, string nextActivityKeyValue)
        {
            // проверим, что ключ с таким именем еще не добавляли
            if (activity.NextActivities.ContainsKey(nextActivityKey))
                throw new WorkflowSchemeParserException(string.Format(
                    "Ключ следующего действия {0} уже добавлен ранее", nextActivityKey.Name), this);

            if (string.IsNullOrEmpty(nextActivityKeyValue))
                throw new WorkflowSchemeParserException(string.Format(
                    "Не задано значение ключа следующего действия {0}", nextActivityKey.Name), this);

            // если значение ключа - это имя встроенной функции
            if (nextActivityKeyValue.StartsWith("@@"))
            {
                nextActivityKeyValue = nextActivityKeyValue.Substring(2);

                if (string.IsNullOrEmpty(nextActivityKeyValue))
                    throw new WorkflowSchemeParserException(string.Format(
                        "Некорректное значение ключа следующего действия {0}: {1}",
                        nextActivityKey.Name, nextActivityKeyValue), this);

                ReturnActivity returnActivity;
                try
                {
                    returnActivity = WorkflowBuiltinFunctions.GetReturnActivity(nextActivityKeyValue);
                }
                catch (Exception ex)
                {
                    throw new WorkflowSchemeParserException(string.Format(
                        "Ошибка получения встроенной функции по выражению {0}", nextActivityKeyValue), ex, this);
                }

                activity.NextActivities.Add(nextActivityKey, returnActivity);
            }
            // иначе, значение ключа - это имя другого действия, которое должно также входить 
            // в состав данного составного действия, что и текущее действие
            else
            {
                // получим полное имя другого действия
                var nextActivityFullName = CreateFullActivityName(nextActivityKeyValue, activity.Parent);

                activity.NextActivities.Add(
                    nextActivityKey,
                    Scheme.Activities.ContainsKey(nextActivityFullName)
                        ? Scheme.Activities[nextActivityFullName]
                        : new UnevaluatedActivity(nextActivityKeyValue, activity.Parent));
            }
        }

        #endregion

        #region Действия-ссылки

        /// <summary>
        /// Читает действие-ссылку
        /// </summary>
        /// <param name="parentActivity"></param>
        private ReferenceActivity ReadReferenceActivity(CompositeActivity parentActivity)
        {
            // создадим действие
            var activity = new ReferenceActivity();

            // прочитаем общие атрибуты
            string activityExecutionMethod;
            ReadActivityCommon(activity, parentActivity, out activityExecutionMethod);

            // пропишем действие для выполнения
            activity.ActivityForExecute = GetReferencedActivity(activityExecutionMethod);
            
            return activity;
        }

        /// <summary>
        /// Возвращает действие, на которое указывает ссылка
        /// </summary>
        /// <param name="referencedActivityFullName"></param>
        /// <returns></returns>
        private Activity GetReferencedActivity(string referencedActivityFullName)
        {
            if (string.IsNullOrEmpty(referencedActivityFullName))
                throw new WorkflowSchemeParserException(
                    "Не задано имя действия, на которое ссылается действие-ссылка", this);

            // найдем действие, на которое указывает ссылка, в схеме
            if (Scheme.Activities.ContainsKey(referencedActivityFullName))
            {
                return Scheme.Activities[referencedActivityFullName];
            }
            
            // если данное действие еще не добавлено в схему, то вернем специальное фиктивное действие,
            // чтобы после завершения разбора всей схемы заменить его на реальное
            return new UnevaluatedActivity(referencedActivityFullName, null);
        }

        #endregion

        #region Работа с событиями

        /// <summary>
        /// Читает действие-подписчик на событие
        /// </summary>
        /// <param name="parentActivity"></param>
        private SubscribeToEventActivity ReadSubscribeToEventActivity(CompositeActivity parentActivity)
        {
            // создадим действие
            var activity = new SubscribeToEventActivity();

            // читаем атрибуты
            var atts = ReadAttributes(new[] { ATT_NAME, ATT_EVENT, ATT_HANDLER, ATT_HANDLINGTYPE, ATT_NEXTACTIVITY });

            // инициализируем действие
            InitEventHandlerActivity(activity, atts, parentActivity);

            // получим тип обработки события
            activity.HandlingType = GetEventHandlingType(atts[ATT_HANDLINGTYPE]);

            return activity;
        }

        /// <summary>
        /// Читает действие-отписчик от события
        /// </summary>
        /// <param name="parentActivity"></param>
        private UnsubscribeFromEventActivity ReadUnsubscribeFromEventActivity(
            CompositeActivity parentActivity)
        {
            // создадим действие
            var activity = new UnsubscribeFromEventActivity();

            // читаем атрибуты
            var atts = ReadAttributes(new[] { ATT_NAME, ATT_EVENT, ATT_HANDLER, ATT_NEXTACTIVITY });

            // инициализируем действие
            InitEventHandlerActivity(activity, atts, parentActivity);

            return activity;
        }

        /// <summary>
        /// Инициализирует действие-обработчик события
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="atts"></param>
        /// <param name="parentActivity"></param>
        private void InitEventHandlerActivity(
            EventHandlerActivity activity, StringDictionary atts, CompositeActivity parentActivity)
        {
            var activityName = atts[ATT_NAME];
            var eventAttValue = atts[ATT_EVENT];
            var handlerAttValue = atts[ATT_HANDLER];
            var nextActivityAttValue = atts[ATT_NEXTACTIVITY];

            // проверим, что все необходимые атрибуты заданы
            if (activityName == null)
                throw new WorkflowSchemeParserException("Не задано имя действия", this);
            if (eventAttValue == null)
                throw new WorkflowSchemeParserException("Не задано событие", this);
            if (handlerAttValue == null)
                throw new WorkflowSchemeParserException("Не задано действие-обработчик события", this);

            // пропишем имя
            activity.Name = CreateFullActivityName(activityName, parentActivity);

            // добавим действие в схему
            AddActivityToScheme(activity);

            // добавим действие в список дочерних составного действия
            parentActivity.Activities.Add(activity);
            activity.Parent = parentActivity;

            // получим событие
            activity.Event = GetEventHolder(activity, eventAttValue);
            // получим действие-обработчик
            activity.Handler = GetEventHandlerActivity(handlerAttValue, parentActivity);

            // добавим след. действие
            if (!string.IsNullOrEmpty(nextActivityAttValue))
                AddNextActivity(activity, Scheme.DefaultNextActivityKey.Name, nextActivityAttValue);
        }

        /// <summary>
        /// Получить хранитель события
        /// </summary>
        /// <param name="activity">действие</param>
        /// <param name="eventAttValue">значение атрибута, который содержит описание события</param>
        /// <returns></returns>
        private EventHolder GetEventHolder(Activity activity, string eventAttValue)
        {
            if (!eventAttValue.StartsWith("@"))
                throw new WorkflowSchemeParserException(string.Format(
                        "Некорректное имя события {0}", eventAttValue), this);

            eventAttValue = eventAttValue.Substring(1);
            int dotIndex = eventAttValue.IndexOf(PREFIX_DELIMITER);

            // если имя содержит префикс, который записывается вначале через точку
            if (dotIndex > 0)
            {
                var strArr = eventAttValue.Split(PREFIX_DELIMITER);
                var prefix = strArr[0];
                var eventName = strArr[1];

                if (string.IsNullOrEmpty(eventName))
                    // т.е. значение = "@Префикс."
                    throw new WorkflowSchemeParserException(string.Format(
                        "Некорректное имя события {0}", eventAttValue), this);

                // определим по префиксу действие, на кот. ссылаются

                // если это ссылка на св-во корневого составного действия
                if (prefix == PREFIX_ROOT)
                    prefix = Scheme.RootActivityName;

                // найдем действие по имени
                var propOwner = GetReferencedActivity(prefix);

                return GetEventHolderForActivityEvent(eventName, propOwner);
            }
            
            // иначе - это ссылка на св-во родительского составного действия
            if (activity.Parent == null)
                throw new WorkflowSchemeParserException(string.Format(
                    "Событие {0} не определено в родительском действии", eventAttValue), this);

            return GetEventHolderForActivityEvent(eventAttValue, activity.Parent);
        }

        /// <summary>
        /// Возвращает держатель события для события, которое определено в заданном действие
        /// </summary>
        /// <param name="eventName">имя события</param>
        /// <param name="eventOwner">действие, в котором должно быть определено событие</param>
        /// <returns></returns>
        private EventHolder GetEventHolderForActivityEvent(string eventName, Activity eventOwner)
        {
            var unevalActivity = eventOwner as UnevaluatedActivity;
            return unevalActivity != null
                       ? new UnevaluatedEventHolder(eventName, unevalActivity)
                       : CreateEventHolder(eventName, eventOwner);
        }

        /// <summary>
        /// Создает держатель осбытия
        /// </summary>
        /// <param name="eventName">имя события</param>
        /// <param name="eventOwner">владелец события</param>
        /// <returns></returns>
        private EventHolder CreateEventHolder(string eventName, Activity eventOwner)
        {
            // найдем событие в типе объекта eventOwner
            var type = eventOwner.GetType();
            EventInfo evInfo;

            try
            {
                evInfo = type.GetEvent(eventName);
                if (evInfo == null)
                    throw new Exception(string.Format("Тип {0} не содержит события public {1}", type.Name, eventName));
            }
            catch (Exception ex)
            {
                throw new WorkflowSchemeParserException(string.Format(
                    "Ошибка получения события {0}", eventName), ex, this);
            }

            return new EventHolder(evInfo, eventOwner);
        }

        /// <summary>
        /// Получить действие-обработчик события по полному имени действия
        /// </summary>
        /// <param name="handlerActivityName"></param>
        /// <param name="compositeActivity"></param>
        /// <returns></returns>
        private Activity GetEventHandlerActivity(string handlerActivityName, CompositeActivity compositeActivity)
        {
            return Scheme.Activities.ContainsKey(handlerActivityName)
                       ? Scheme.Activities[handlerActivityName]
                       : new UnevaluatedActivity(handlerActivityName, compositeActivity);
        }

        /// <summary>
        /// Получить тип обработки события
        /// </summary>
        /// <param name="handlingTypeName">название типа</param>
        /// <returns></returns>
        private EventHandlingType GetEventHandlingType(string handlingTypeName)
        {
            if (string.IsNullOrEmpty(handlingTypeName))
                return EventHandlingType.Sync;

            try
            {
                return (EventHandlingType)Enum.Parse(typeof(EventHandlingType), handlingTypeName);
            }
            catch (Exception ex)
            {
                throw new WorkflowSchemeParserException(
                    "Некорректно задан тип обработки события: " + handlingTypeName, ex, this);
            }
        }
        
        #endregion

        #region Блокировки

        /// <summary>
        /// Читает действие, которое получает блокировку
        /// </summary>
        /// <param name="parentActivity"></param>
        private MonitorEnterActivity ReadMonitorEnterActivity(CompositeActivity parentActivity)
        {
            // создадим действие
            var activity = new MonitorEnterActivity();

            // инициализируем действие
            InitMonitorActivity(activity, parentActivity);

            return activity;
        }

        /// <summary>
        /// Читает действие, которое освобождает блокировку
        /// </summary>
        /// <param name="parentActivity"></param>
        private MonitorExitActivity ReadMonitorExitActivity(CompositeActivity parentActivity)
        {
            // создадим действие
            var activity = new MonitorExitActivity();

            // инициализируем действие
            InitMonitorActivity(activity, parentActivity);

            return activity;
        }

        /// <summary>
        /// Инициализирует действие для работы с блокировкой
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="parentActivity"></param>
        private void InitMonitorActivity(MonitorActivity activity, CompositeActivity parentActivity)
        {
            // читаем атрибуты
            var atts = ReadAttributes(new[] { ATT_NAME, ATT_LOCKNAME, ATT_NEXTACTIVITY });

            var activityName = atts[ATT_NAME];
            var lockName = atts[ATT_LOCKNAME];
            var nextActivityAttValue = atts[ATT_NEXTACTIVITY];

            // проверим, что все необходимые атрибуты заданы
            if (activityName == null)
                throw new WorkflowSchemeParserException("Не задано имя действия", this);
            if (lockName == null)
                throw new WorkflowSchemeParserException("Не задано имя блокировки", this);

            // пропишем имя
            activity.Name = CreateFullActivityName(activityName, parentActivity);
            // пропишем имя блокировки
            activity.LockName = lockName;

            // добавим действие в схему
            AddActivityToScheme(activity);

            // добавим действие в список дочерних составного действия
            parentActivity.Activities.Add(activity);
            activity.Parent = parentActivity;

            // добавим след. действие
            if (!string.IsNullOrEmpty(nextActivityAttValue))
                AddNextActivity(activity, Scheme.DefaultNextActivityKey.Name, nextActivityAttValue);
        }

        #endregion

        #endregion

        #region Пост-парсинговые вычисления

        /// <summary>
        /// Вычисляет ранее невычисленное действие
        /// </summary>
        /// <param name="unevaluatedActivity"></param>
        /// <param name="errorMessagePrefix"></param>
        /// <returns></returns>
        private Activity EvaluateActivity(UnevaluatedActivity unevaluatedActivity, string errorMessagePrefix)
        {
            // сначала попробуем найти по имени действия, т.к. оно уже может быть полным
            if (Scheme.Activities.ContainsKey(unevaluatedActivity.ActivityName))
                return Scheme.Activities[unevaluatedActivity.ActivityName];

            // не нашли => попробуем найти по полному имени
            if (unevaluatedActivity.ParentActivity != null)
            {
                var fullActivityName = CreateFullActivityName(
                    unevaluatedActivity.ActivityName, unevaluatedActivity.ParentActivity);
                if (Scheme.Activities.ContainsKey(fullActivityName))
                    return Scheme.Activities[fullActivityName];
            }

            // так и не не нашли
            throw new WorkflowSchemeParserException(string.Format(
                "{0}: не найдено действие с именем {1}", errorMessagePrefix, unevaluatedActivity.ActivityName), this);
        }

        /// <summary>
        /// Вычисляет следующие действия, которые не еще не вычислены
        /// </summary>
        private void EvaluateNextActivities()
        {
            // вычисленные след. действия - список троек 
            // [действие, для кот. вычислено след. действие],[ключ след. действия],[вычисленное след. действие]
            var evaluatedNextActivities = new List<Triplet<Activity, NextActivityKey, Activity>>();

            foreach (var activity in Scheme.Activities.Values)
            {
                foreach (var entry in activity.NextActivities)
                {
                    var nextActivity = entry.Value;

                    // если след. действие не вычислено
                    var unevalNextActivity = nextActivity as UnevaluatedActivity;
                    if (unevalNextActivity != null)
                        // запомним, где и на что нужно заменить невычисленное след. действие
                        evaluatedNextActivities.Add(new Triplet<Activity, NextActivityKey, Activity>(
                            activity, entry.Key,
                            EvaluateActivity(
                                unevalNextActivity,
                                string.Format("Вычисление следующего действия для действия {0}", activity.Name))));
                }
            }

            // заменим невычисленные след. действия на вычисленные
            foreach (var triplet in evaluatedNextActivities)
                triplet.First.NextActivities[triplet.Second] = triplet.Third;
        }

        /// <summary>
        /// Проверяет корректность задания следующих действий
        /// </summary>
        private void ValidateNextActivities()
        {
            foreach (var activity in Scheme.Activities.Values)
            {
                // проверим, что у текущего действия все ключи след. действий не указывают на одно и то же действие
                if (activity.NextActivities.Count > 1)
                {
                    var enumerator = activity.NextActivities.GetEnumerator();
                    enumerator.MoveNext();

                    var firstName = enumerator.Current.Value.Name;
                    var allEquals = true;

                    while (enumerator.MoveNext())
                    {
                        if (string.CompareOrdinal(firstName, enumerator.Current.Value.Name) != 0)
                        {
                            allEquals = false;
                            break;
                        }
                    }

                    // если все ключи след. действий указывают на одно и то же действие
                    if (allEquals)
                    {
                        // запишем в лог предупреждение
                        CoreApplication.Instance.Logger.LogWarning(
                                "Для действия '{0}' все ключи следующих действий имеют одинаковое значение: {1}",
                                activity.Name, firstName);
                    }
                }
            }
        }

        /// <summary>
        /// Вычисляет действия-обработчики событий, которые еще не вычислены
        /// </summary>
        private void EvaluateEventHandlerActivities()
        {
            foreach (var activity in Scheme.Activities.Values)
            {
                var ehActivity = activity as EventHandlerActivity;
                if (ehActivity == null)
                    continue;

                var unevalHandlerActivity = ehActivity.Handler as UnevaluatedActivity;
                if (unevalHandlerActivity != null)
                    // исправим ссылку на реальное действие
                    ehActivity.Handler = EvaluateActivity(
                        unevalHandlerActivity,
                        string.Format("Вычисление действия-обработчика для действия {0}", ehActivity.Name));
            }
        }

        /// <summary>
        /// Вычисляет действия, на которые ссылаются действия-ссылки, которые еще не вычислены
        /// </summary>
        private void EvaluateReferencedActivities()
        {
            foreach (var activity in Scheme.Activities.Values)
            {
                var refActivity = activity as ReferenceActivity;
                if (refActivity == null)
                    continue;

                var unevalRefActivity = refActivity.ActivityForExecute as UnevaluatedActivity;
                if (unevalRefActivity != null)
                    // исправим ссылку на реальное действие
                    refActivity.ActivityForExecute = EvaluateActivity(
                        unevalRefActivity,
                        string.Format("Вычисление действия-ссылки для действия {0}", refActivity.Name));
            }
        }

        /// <summary>
        /// Вычисляет вычислители значений параметров, которые не смогли вычислить сразу же при чтении схемы 
        /// </summary>
        private void EvaluateParameterEvaluators()
        {
            foreach (var activity in Scheme.Activities.Values)
                foreach (var param in activity.Parameters)
                    param.Evaluator = EvaluateParameterEvaluator(param.Evaluator);
        }

        /// <summary>
        /// Вычисляет вычислитель значения для конкретного параметра
        /// </summary>
        /// <param name="evaluator"></param>
        /// <returns></returns>
        private ActivityParameterEvaluator EvaluateParameterEvaluator(ActivityParameterEvaluator evaluator)
        {
            if (evaluator.ValueType == ActivityParameterValueType.Array)
            {
                for (int i = 0; i < evaluator.EvaluatorArray.Length; i++)
                    evaluator.EvaluatorArray[i] = EvaluateParameterEvaluator(evaluator.EvaluatorArray[i]);

                return evaluator;
            }

            var unevalEvaluator = evaluator as UnevaluatedActivityParameterEvaluator;
            if (unevalEvaluator == null)
                return evaluator;

            return CreateActivityParameterEvaluator(
                unevalEvaluator.PropertyName, 
                EvaluateActivity(
                    unevalEvaluator.PropertyOwner,
                    string.Format("Вычисление действия, содержащего св-во {0}", unevalEvaluator.PropertyName)));
        }

        /// <summary>
        /// Вычисляет держатели событий, которые не смогли вычислить сразу же при чтении схемы 
        /// </summary>
        private void EvaluateEventHolders()
        {
            foreach (var activity in Scheme.Activities.Values)
            {
                var ehActivity = activity as EventHandlerActivity;
                if (ehActivity == null)
                    continue;

                var unevalEventHolder = ehActivity.Event as UnevaluatedEventHolder;
                if (unevalEventHolder != null)
                    // заменим держатель события на настоящий
                    ehActivity.Event = CreateEventHolder(
                        unevalEventHolder.EventName,
                        EvaluateActivity(
                            unevalEventHolder.EventOwner,
                            string.Format("Вычисление действия, содержащего событие {0}", unevalEventHolder.EventName)));
            }
        }

        #endregion

        #region Проверки результата парсинга

        /// <summary>
        /// Проверяет валидность имен параметров
        /// </summary>
        private void CheckParametersNames()
        {
            foreach (var activity in Scheme.Activities.Values)
                foreach (var param in activity.Parameters)
                {
                    // если параметр задается для вызова другого действия
                    if (activity is ReferenceActivity)
                    {
                        var refActivity = (ReferenceActivity)activity;

                        // и это другое действие - составное действие
                        if (refActivity.ActivityForExecute is CompositeActivity)
                        {
                            // то в классе этого составного действия должно быть объявлено соотв. св-во
                            CheckPropertyForParameter(refActivity.ActivityForExecute, param.Name);

                            // если 
                            if (// это параметр, который задает начальное действие,
                                string.CompareOrdinal(param.Name, PARAM_STARTACTIVITY) == 0 &&
                                // И значение параметра задано явно
                                param.Evaluator.ValueType == ActivityParameterValueType.PlainValue)
                            {
                                // то проверим, что действие с таким именем действительно есть 
                                // среди вложенных действий этого составного действия

                                var compositeActivity = (CompositeActivity) refActivity.ActivityForExecute;
                                var startActivityName = (string) param.GetValue();
                                var startActivityFullName = CreateFullActivityName(
                                    startActivityName, compositeActivity.Name);

                                if (!compositeActivity.Activities.ContainsKey(startActivityFullName))
                                    throw new WorkflowSchemeParserException(string.Format(
                                        "Действие с именем '{0}' не найдено среди действий составного действия '{1}'",
                                        startActivityName, compositeActivity.Name), this);
                            }
                        }
                    }
                    // если параметр задается для составного действия
                    else if (activity is CompositeActivity)
                    {
                        var compositeActivity = (CompositeActivity)activity;

                        // то в классе этого составного действия должно быть объявлено соотв. св-во
                        CheckPropertyForParameter(compositeActivity, param.Name);
                    }
                }
        }

        /// <summary>
        /// Выполняет проверку, что в классе действия объявлено св-во для параметра
        /// </summary>
        /// <param name="propertyOwner"></param>
        /// <param name="paramName"></param>
        private void CheckPropertyForParameter(Activity propertyOwner, string paramName)
        {
            var type = propertyOwner.GetType();

            try
            {
                type.GetProperty(paramName, true, true);
            }
            catch (Exception ex)
            {
                throw new WorkflowSchemeParserException(string.Format(
                    "Для параметра {0} не определено свойство public {0} {{get;set;}} в классе {1} действия {2}",
                    paramName, type.FullName, propertyOwner.Name), ex, this);
            }
        }

        /// <summary>
        /// Проверка корректности схемы
        /// </summary>
        private void ValidateScheme()
        {
            if (string.IsNullOrEmpty(Scheme.RootActivityName))
                throw new WorkflowSchemeParserException("Не определено имя корневого действия", this);

            if (!Scheme.Activities.ContainsKey(Scheme.RootActivityName) ||
                !(Scheme.Activities[Scheme.RootActivityName] is CompositeActivity))
                throw new WorkflowSchemeParserException(
                    string.Format("Не найдено корневое составное действие: {0}", Scheme.RootActivityName), this);
        }

        /// <summary>
        /// Добавляет в схему действие-выход, выполнение которого приводит к завершению выполнения потока работ
        /// </summary>
        private void AddExitActivity()
        {
            var exitActivity = new ReturnActivity(Scheme.DefaultNextActivityKey)
                                   {
                                       Name = "ExitActivity",
                                       Priority = ActivityPriority.Highest
                                   };

            // добавим действие-выход в схему
            AddActivityToScheme(exitActivity);
            Scheme.ExitActivity = exitActivity;

            // добавим действие-выход в список дочерних корневого действия
            var rootActivity = (CompositeActivity) Scheme.RootActivity;
            rootActivity.Activities.Add(exitActivity);
            exitActivity.Parent = rootActivity;
        }

        #endregion

        #region Native-классы

        /// <summary>
        /// Вспомогательный класс, который используется для "погружения" в регионы и выхода их них
        /// </summary>
        private class RegionDiver
        {
            /// <summary>
            /// Xml-читатель
            /// </summary>
            private readonly XmlReaderEx _reader;
            /// <summary>
            /// Глубина текущего региона
            /// </summary>
            private int _depth;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="reader"></param>
            public RegionDiver(XmlReaderEx reader)
            {
                _reader = reader;
            }

            /// <summary>
            /// Войти в регион
            /// </summary>
            /// <returns></returns>
            public void EnterToRegion()
            {
                _reader.DownDepth();
                _depth++;
            }

            /// <summary>
            /// Выйти из текущего региона
            /// </summary>
            /// <returns>
            /// true - выход из региона выполнен, 
            /// false - выйти нельзя, т.к. не был выполнен вход в регион
            /// </returns>
            public bool ExitFromRegion()
            {
                if (_depth == 0)
                    return false;

                _reader.UpDepth();
                _depth--;

                return true;
            }

            /// <summary>
            /// Выйти из всех регионов
            /// </summary>
            public void ExitFromAllRegions()
            {
                while (ExitFromRegion())
                {
                }
            }
        }

        #endregion
    }
}
