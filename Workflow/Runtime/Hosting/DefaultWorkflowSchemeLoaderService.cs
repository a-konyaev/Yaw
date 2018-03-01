using System.Collections.Generic;
using System.Xml;
using Yaw.Core;
using Yaw.Workflow.ComponentModel;
using Yaw.Workflow.ComponentModel.Compiler;

namespace Yaw.Workflow.Runtime.Hosting
{
    /// <summary>
    /// Загрузчик схемы потока работ по умолчанию
    /// </summary>
    public class DefaultWorkflowSchemeLoaderService : WorkflowSchemeLoaderService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected internal override WorkflowScheme CreateInstance(
            string workflowSchemeUri, IEnumerable<KeyValuePair<string, XmlReader>> customXmlSchemas)
        {
            CodeContract.Requires(!string.IsNullOrEmpty(workflowSchemeUri));

            var parser = new WorkflowSchemeParser();
            parser.Parse(workflowSchemeUri, customXmlSchemas);

            return parser.Scheme;
        }
    }
}
