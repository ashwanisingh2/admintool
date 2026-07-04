using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Core.Interfaces;

public interface IBsodAnalyzerService
{
    Task<Result<List<BsodEntryModel>>> AnalyzeDumpsAsync(CancellationToken ct);
    Task<Result<string>> GenerateHtmlReportAsync(IEnumerable<BsodEntryModel> entries, CancellationToken ct);
}
