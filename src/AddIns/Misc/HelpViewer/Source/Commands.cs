﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using MSHelpSystem.Core;
using MSHelpSystem.Controls;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;

namespace MSHelpSystem.Commands
{
	public class ShowErrorHelpCommand : AbstractMenuCommand
	{
		public override void Run()
		{
			var view = (System.Windows.Controls.ListView)Owner;
			foreach (var t in view.SelectedItems.OfType<SDTask>().ToArray()) {
				if (t.BuildError == null)
					continue;

				string code = t.BuildError.ErrorCode;
				if (string.IsNullOrEmpty(code))
					return;

				if (Help3Environment.IsHelp3ProtocolRegistered) {
					LoggingService.Debug(string.Format("Help 3.0: Getting description of \"{0}\"", code));
					if (Help3Environment.IsLocalHelp)
						DisplayHelp.Keywords(code);
					else
						DisplayHelp.ContextualHelp(code);
				} else {
					LoggingService.Error("Help 3.0: Help system ist not initialized");
				}
			}
		}
	}
	
	public class DisplayContent : AbstractMenuCommand
	{
		public override void Run()
		{
			if (Help3Service.Config.ExternalHelp) DisplayHelp.Catalog();
			else {
				PadDescriptor toc = SD.Workbench.GetPad(typeof(Help3TocPad));
				if (toc != null) toc.BringPadToFront();
			}
		}
	}

	public class DisplaySearch : AbstractMenuCommand
	{
		public override void Run()
		{
			PadDescriptor search = SD.Workbench.GetPad(typeof(Help3SearchPad));
			if (search != null) search.BringPadToFront();
		}
	}
	
	public class LaunchHelpLibraryManager : AbstractMenuCommand
	{
		public override void Run()
		{
			string path;
			using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Help\v1.0")) {
				path = key != null ? key.GetValue("AppRoot") as string : null;
			}
			if (string.IsNullOrEmpty(path)) {
				MessageService.ShowError("${res:AddIns.HelpViewer.HLMNotFound}");
				return;
			}
			path = Path.Combine(path, "HelpLibManager.exe");
			if (!File.Exists(path)) {
				MessageService.ShowError("${res:AddIns.HelpViewer.HLMNotFound}");
				return;
			}
			if (string.IsNullOrEmpty(Help3Service.Config.ActiveCatalogId)) {
				MessageService.ShowError("${res:AddIns.HelpViewer.HLMNoActiveCatalogError}");
				return;
			}
			Process.Start(path, string.Format("/product {0} /version {1} /locale {2}", Help3Service.Config.ActiveCatalogId.Split('/')));
		}
	}
}
