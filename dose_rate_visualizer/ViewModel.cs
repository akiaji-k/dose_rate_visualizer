using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

using dose_rate_visualizer.Models;
using System.ComponentModel;
using System.Xml.Linq;

namespace dose_rate_visualizer.ViewModels
{
    internal class ViewModel
    {
        /* Model instance */
        public Model InstModel { get; } = new Model();


        public ViewModel() { }
        internal void SetScriptContextToModel(in ScriptContext context)
                    //internal void SetScriptContextToModel()

        {
            InstModel.SetScriptContext(context);
        }

     }

}
