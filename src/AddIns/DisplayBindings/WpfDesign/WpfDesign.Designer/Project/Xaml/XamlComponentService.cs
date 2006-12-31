﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using ICSharpCode.WpfDesign.XamlDom;
using System.Windows.Markup;

namespace ICSharpCode.WpfDesign.Designer.Xaml
{
	sealed class XamlComponentService : IComponentService
	{
		#region IdentityEqualityComparer
		sealed class IdentityEqualityComparer : IEqualityComparer<object>
		{
			internal static readonly IdentityEqualityComparer Instance = new IdentityEqualityComparer();
			
			int IEqualityComparer<object>.GetHashCode(object obj)
			{
				return obj.GetHashCode();
			}
			
			bool IEqualityComparer<object>.Equals(object x, object y)
			{
				return x == y;
			}
		}
		#endregion
		
		readonly XamlDesignContext _context;
		
		public XamlComponentService(XamlDesignContext context)
		{
			this._context = context;
		}
		
		public event EventHandler<DesignItemEventArgs> ComponentRegistered;
		public event EventHandler<DesignItemEventArgs> ComponentUnregistered;
		
		Dictionary<object, XamlDesignItem> _sites = new Dictionary<object, XamlDesignItem>(IdentityEqualityComparer.Instance);
		
		public DesignItem GetDesignItem(object component)
		{
			if (component == null)
				throw new ArgumentNullException("component");
			XamlDesignItem site;
			_sites.TryGetValue(component, out site);
			return site;
		}
		
		public DesignItem RegisterComponentForDesigner(object component)
		{
			if (component == null) {
				component = new NullExtension();
			} else if (component is Type) {
				component = new TypeExtension((Type)component);
			}
			
			XamlDesignItem item = new XamlDesignItem(_context.Document.CreateObject(component), _context);
			_sites.Add(component, item);
			return item;
		}
		
		/// <summary>
		/// currently for use by UnregisterAllComponents only because it doesn't update the XAML
		/// </summary>
		void UnregisterComponentFromDesigner(DesignItem site)
		{
			if (site == null)
				throw new ArgumentNullException("site");
			
			if (!_sites.Remove(site.Component))
				throw new ArgumentException("The site was not registered here!");
			
			if (ComponentUnregistered != null) {
				ComponentUnregistered(this, new DesignItemEventArgs(site));
			}
		}
		
		/// <summary>
		/// registers components from an existing XAML tree
		/// </summary>
		internal XamlDesignItem RegisterXamlComponentRecursive(XamlObject obj)
		{
			if (obj == null) return null;
			
			foreach (XamlProperty prop in obj.Properties) {
				RegisterXamlComponentRecursive(prop.PropertyValue as XamlObject);
				foreach (XamlPropertyValue val in prop.CollectionElements) {
					RegisterXamlComponentRecursive(val as XamlObject);
				}
			}
			
			XamlDesignItem site = new XamlDesignItem(obj, _context);
			_sites.Add(site.Component, site);
			if (ComponentRegistered != null) {
				ComponentRegistered(this, new DesignItemEventArgs(site));
			}
			return site;
		}
		
		/// <summary>
		/// unregisters all components
		/// </summary>
		internal void UnregisterAllComponents()
		{
			Array.ForEach(Linq.ToArray(_sites.Values), UnregisterComponentFromDesigner);
			_sites.Clear();
		}
	}
}
