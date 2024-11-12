using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.UI.App.Common;

namespace Ryujinx.UI.Common.Models
{
    public record XCITrimmerFileModel(
        string Name,
        string Path,
        bool Trimmable,
        bool Untrimmable,
        long PotentialSavingsB,
        long CurrentSavingsB,
        int? PercentageProgress,
        XCIFileTrimmer.OperationOutcome ProcessingOutcome)
    {
        public static XCITrimmerFileModel FromApplicationData(ApplicationData applicationData, XCIFileTrimmerLog logger)
        {
            var trimmer = new XCIFileTrimmer(applicationData.Path, logger);

            return new XCITrimmerFileModel(
                applicationData.Name,
                applicationData.Path,
                trimmer.CanBeTrimmed,
                trimmer.CanBeUntrimmed,
                trimmer.DiskSpaceSavingsB,
                trimmer.DiskSpaceSavedB,
                null,
                XCIFileTrimmer.OperationOutcome.Undetermined
            );
        }

        public bool IsFailed
        {
            get
            {
                return ProcessingOutcome != XCIFileTrimmer.OperationOutcome.Undetermined &&
                    ProcessingOutcome != XCIFileTrimmer.OperationOutcome.Successful;
            }
        }

        public virtual bool Equals(XCITrimmerFileModel obj)
        {
            if (obj == null)
                return false;
            
            return this.Path == obj.Path;
        }
        
        public override int GetHashCode()
        {
            return this.Path.GetHashCode();
        }
    }
}
