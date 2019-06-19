using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MobileEssentials
{
    [ProvideBindingPath]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid("0553379B-5124-48B6-80B0-09AF3A331CDD")]
    [ProvideAutoLoad(SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class HangFixerPackage : AsyncPackage, IVsSolutionLoadEvents, IVsSolutionEvents
    {
        /// <summary>Specifies a context in which a solution is being opened.</summary>
        public const string SolutionOpening_string = "{D2567162-F94F-4091-8798-A096E61B8B50}";

        private string flagFile;

        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            IVsSolution2 solution = (IVsSolution2)await GetServiceAsync(typeof(SVsSolution));
            Assumes.Present(solution);

            //System.Windows.Threading.Dispatcher.VerifyAccess();
            ErrorHandler.ThrowOnFailure(solution.AdviseSolutionEvents(this, out _));
            await base.InitializeAsync(cancellationToken, progress);
        }

        // Order of callbacks is:
        // OnBeforeOpenSolution > OnAfterOpenSolution > OnBeforeBackgroundSolutionLoadBegins > OnAfterBackgroundSolutionLoadComplete
        // The last two will only happen if background solution load is enabled.
        // Therefore, we will setup the flag file on both befores, and clear it on both afters.
        public int OnBeforeOpenSolution(string pszSolutionFilename)
        {
            // If flag file exists, delete .vs and .suo, since it means we tried loading 
            // before, and we failed.
            // This happens only once for a given solution, so it's safe to assume this 
            // is the "main" entry point event.
            flagFile = Path.ChangeExtension(pszSolutionFilename, ".tmp");

            if (File.Exists(flagFile))
            {
                string vsDir = Path.Combine(Path.GetDirectoryName(pszSolutionFilename), ".vs");
                if (Directory.Exists(vsDir))
                {
                    try
                    {
                        Directory.Delete(vsDir, true);
                    }
                    catch (IOException)
                    {
                        // Never fail, no matter what.
                    }
                }
                foreach (string suoFile in Directory.EnumerateFiles(Path.GetDirectoryName(pszSolutionFilename), "*.suo"))
                {
                    try
                    {
                        File.Delete(suoFile);
                    }
                    catch (IOException)
                    {
                        // Never fail, no matter what.
                    }
                }
            }

            // Written outside the if, so that we always get the 
            // timestamp of the current solution load attempt.
            File.WriteAllText(flagFile, "Visual Studio Hang Fixer");

            // TODO: create the flag file
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            // Load was successfull, clear the flag file.
            ClearFlagFile();
            return VSConstants.S_OK;
        }

        public int OnBeforeBackgroundSolutionLoadBegins()
        {
            // The OnAfterOpenSolution is called even when pending 
            // background solution load is pending (which is optional 
            // depending on user settings). So we write the flag file
            // again to get the timestamp of the current background 
            // solution load attempt.
            File.WriteAllText(flagFile, "Visual Studio Hang Fixer");
            return VSConstants.S_OK;
        }

        public int OnAfterBackgroundSolutionLoadComplete()
        {
            // Load was successfull, clear the flag file.
            ClearFlagFile();
            return VSConstants.S_OK;
        }

        private void ClearFlagFile()
        {
            if (File.Exists(flagFile))
            {
                File.Delete(flagFile);
            }
        }

        #region Unused

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryBackgroundLoadProjectBatch(out bool pfShouldDelayLoadToNextIdle)
        {
            pfShouldDelayLoadToNextIdle = false;
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        #endregion
    }
}