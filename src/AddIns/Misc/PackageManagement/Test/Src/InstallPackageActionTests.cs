﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.PackageManagement;
using ICSharpCode.PackageManagement.Design;
using NuGet;
using NUnit.Framework;
using PackageManagement.Tests.Helpers;

namespace PackageManagement.Tests
{
	[TestFixture]
	public class InstallPackageActionTests
	{
		FakePackageManagementEvents fakePackageManagementEvents;
		FakePackageManagementProject fakeProject;
		InstallPackageAction action;
		InstallPackageHelper installPackageHelper;

		void CreateAction()
		{
			fakePackageManagementEvents = new FakePackageManagementEvents();
			fakeProject = new FakePackageManagementProject();
			action = new InstallPackageAction(fakeProject, fakePackageManagementEvents);
			installPackageHelper = new InstallPackageHelper(action);
		}
		
		FakePackage AddOnePackageToProjectSourceRepository(string packageId)
		{
			return fakeProject.FakeSourceRepository.AddFakePackage(packageId);
		}
		
		void AddInstallOperationWithFile(string fileName)
		{
			var package = new FakePackage();
			package.AddFile(fileName);
			
			var operation = new PackageOperation(package, PackageAction.Install);
			var operations = new List<PackageOperation>();
			operations.Add(operation);
			
			action.Operations = operations;
		}
		
		[Test]
		public void Execute_PackageIsSet_InstallsPackageIntoProject()
		{
			CreateAction();
			installPackageHelper.InstallTestPackage();
			
			var actualPackage = fakeProject.PackagePassedToInstallPackage;
			var expectedPackage = installPackageHelper.TestPackage;
			
			Assert.AreEqual(expectedPackage, actualPackage);
		}
		
		[Test]
		public void Execute_PackageIsSet_InstallsPackageUsingPackageOperations()
		{
			CreateAction();
			var expectedOperations = new List<PackageOperation>();
			installPackageHelper.PackageOperations = expectedOperations;
			installPackageHelper.InstallTestPackage();
			
			var actualOperations = fakeProject.PackageOperationsPassedToInstallPackage;
			
			Assert.AreEqual(expectedOperations, actualOperations);
		}
		
		[Test]
		public void Execute_PackageIsSet_InstallsPackageNotIgnoringDependencies()
		{
			CreateAction();
			installPackageHelper.IgnoreDependencies = false;
			installPackageHelper.InstallTestPackage();
			
			bool ignored = fakeProject.IgnoreDependenciesPassedToInstallPackage;
			
			Assert.IsFalse(ignored);
		}
		
		[Test]
		public void Execute_PackageIsSetAndIgnoreDependencies_IsTrueInstallsPackageIgnoringDependencies()
		{
			CreateAction();
			installPackageHelper.IgnoreDependencies = true;
			installPackageHelper.InstallTestPackage();
			
			bool ignored = fakeProject.IgnoreDependenciesPassedToInstallPackage;
			
			Assert.IsTrue(ignored);
		}
		
		[Test]
		public void IgnoreDependencies_DefaultValue_IsFalse()
		{
			CreateAction();
			Assert.IsFalse(action.IgnoreDependencies);
		}
		
		[Test]
		public void Execute_PackageAndPackageRepositoryPassed_PackageInstallNotificationRaisedWithInstalledPackage()
		{
			CreateAction();
			installPackageHelper.InstallTestPackage();
			
			var expectedPackage = installPackageHelper.TestPackage;
			var actualPackage = fakePackageManagementEvents.PackagePassedToOnParentPackageInstalled;
			
			Assert.AreEqual(expectedPackage, actualPackage);
		}
		
		[Test]
		public void Execute_PackageIdAndSourceAndProjectPassed_PackageOperationsRetrievedFromProject()
		{
			CreateAction();
			fakeProject.AddFakeInstallOperation();
			installPackageHelper.InstallPackageById("PackageId");
			
			var actualOperations = action.Operations;
			var expectedOperations = fakeProject.FakeInstallOperations;
			
			Assert.AreEqual(expectedOperations, actualOperations);
		}
		
		[Test]
		public void Execute_PackageSpecifiedButNoPackageOperations_PackageUsedWhenPackageOperationsRetrievedForProject()
		{
			CreateAction();
			installPackageHelper.PackageOperations = null;
			installPackageHelper.InstallTestPackage();
			
			var expectedPackage = installPackageHelper.TestPackage;
			
			var actualPackage = fakeProject.PackagePassedToGetInstallPackageOperations;
			
			Assert.AreEqual(expectedPackage, actualPackage);
		}
		
		[Test]
		public void Execute_PackageIdAndSourceAndProjectPassedAndIgnoreDependenciesIsTrue_DependenciesIgnoredWhenGettingPackageOperations()
		{
			CreateAction();
			installPackageHelper.IgnoreDependencies = true;
			installPackageHelper.InstallPackageById("PackageId");
			
			bool result = fakeProject.IgnoreDependenciesPassedToGetInstallPackageOperations;
			
			Assert.IsTrue(result);
		}
		
		[Test]
		public void InstallPackage_PackageIdAndSourceAndProjectPassedAndIgnoreDependenciesIsFalse_DependenciesNotIgnoredWhenGettingPackageOperations()
		{
			CreateAction();
			installPackageHelper.IgnoreDependencies = false;
			installPackageHelper.InstallPackageById("PackageId");
			
			bool result = fakeProject.IgnoreDependenciesPassedToGetInstallPackageOperations;
			
			Assert.IsFalse(result);
		}

		[Test]
		public void Execute_VersionSpecified_VersionUsedWhenSearchingForPackage()
		{
			CreateAction();
			
			var recentPackage = AddOnePackageToProjectSourceRepository("PackageId");
			recentPackage.Version = new Version("1.2.0");
			
			var oldPackage = AddOnePackageToProjectSourceRepository("PackageId");
			oldPackage.Version = new Version("1.0.0");
			
			var package = AddOnePackageToProjectSourceRepository("PackageId");
			var version = new Version("1.1.0");
			package.Version = version;
			
			installPackageHelper.Version = version;
			installPackageHelper.InstallPackageById("PackageId");
			
			var actualPackage = fakeProject.PackagePassedToInstallPackage;
			
			Assert.AreEqual(package, actualPackage);
		}
		
		[Test]
		public void HasPackageScriptsToRun_OnePackageInOperationsHasInitPowerShellScript_ReturnsTrue()
		{
			CreateAction();
			AddInstallOperationWithFile(@"tools\init.ps1");
			
			bool hasPackageScripts = action.HasPackageScriptsToRun();
			
			Assert.IsTrue(hasPackageScripts);
		}
		
		[Test]
		public void HasPackageScriptsToRun_OnePackageInOperationsHasNoFiles_ReturnsFalse()
		{
			CreateAction();
			action.Operations = new List<PackageOperation>();
			
			bool hasPackageScripts = action.HasPackageScriptsToRun();
			
			Assert.IsFalse(hasPackageScripts);
		}
		
		[Test]
		public void HasPackageScriptsToRun_OnePackageInOperationsHasInitPowerShellScriptInUpperCase_ReturnsTrue()
		{
			CreateAction();
			AddInstallOperationWithFile(@"tools\INIT.PS1");
			
			bool hasPackageScripts = action.HasPackageScriptsToRun();
			
			Assert.IsTrue(hasPackageScripts);
		}
		
		[Test]
		public void HasPackageScriptsToRun_OnePackageInOperationsHasInstallPowerShellScriptInUpperCase_ReturnsTrue()
		{
			CreateAction();
			AddInstallOperationWithFile(@"tools\INSTALL.PS1");
			
			bool hasPackageScripts = action.HasPackageScriptsToRun();
			
			Assert.IsTrue(hasPackageScripts);
		}
		
		[Test]
		public void HasPackageScriptsToRun_OnePackageInOperationsHasUninstallPowerShellScriptInUpperCase_ReturnsTrue()
		{
			CreateAction();
			AddInstallOperationWithFile(@"tools\UNINSTALL.PS1");
			
			bool hasPackageScripts = action.HasPackageScriptsToRun();
			
			Assert.IsTrue(hasPackageScripts);
		}
	}
}