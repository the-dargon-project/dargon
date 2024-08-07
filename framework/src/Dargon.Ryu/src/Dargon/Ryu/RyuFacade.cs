﻿using Dargon.Ryu.Internals;
using System;
using System.Collections.Generic;

namespace Dargon.Ryu {
   public class RyuFacade : IRyuFacade {
      private readonly IRyuContainer container;
      private readonly IActivator activator;
      private readonly IModuleImporter moduleImporter;

      public RyuFacade(IRyuContainer container, IActivator activator, IModuleImporter moduleImporter) {
         this.container = container;
         this.activator = activator;
         this.moduleImporter = moduleImporter;
      }

      public void Initialize() {
         container.Set(typeof(IRyuFacade), this);
         container.Set(typeof(RyuFacade), this);
      }

      public IRyuContainer Container => container;
      public bool TryGet(Type type, out object value) => container.TryGet(type, out value);
      public object GetOrActivate(Type type) => container.GetOrActivate(type);
      public object ActivateUntracked(Type type) => container.ActivateUntracked(type);
      public IEnumerable<object> Find(Type queryType) => container.Find(queryType);
      public void Set(Type type, object instance) => container.Set(type, instance);
      public IRyuContainer CreateChildContainer(string name) => container.CreateChildContainer(name);

      public IActivator Activator => activator;

      public IModuleImporter ModuleImporter => moduleImporter;
   }
}
