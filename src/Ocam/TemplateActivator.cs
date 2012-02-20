using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RazorEngine.Templating;

namespace Ocam
{
    public class TemplateActivator : IActivator
    {
        ISiteContext _context;

        public TemplateActivator(ISiteContext context)
        {
            _context = context;
        }

        public ITemplate CreateInstance(InstanceContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            var instance = context.Loader.CreateInstance(context.TemplateType);
            var configurable = instance as IConfigurable;
            if (configurable != null)
            {
                configurable.Configure(_context);
            }
            return instance;
        }
    }
}
