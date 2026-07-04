using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Core.Interfaces;

public interface IOneClickCareService
{
    event EventHandler<StepProgressEventArgs>? StepProgressChanged;
    Task RunCareSequenceAsync(IEnumerable<CareStepModel> steps, CancellationToken ct);
}
