namespace Ryujinx.Ava.Systems.PlayReport
{
    /// <summary>
    /// The delegate type that powers single value formatters.<br/>
    /// Takes in the result value from the Play Report, and outputs:
    /// <br/>
    /// a formatted string,
    /// <br/>
    /// a signal that nothing was available to handle it,
    /// <br/>
    /// OR a signal to reset the value that the caller is using the <see cref="Analyzer"/> for. 
    /// </summary>
    public delegate FormattedValue SingleValueFormatter(SingleValue value);

    /// <summary>
    /// The delegate type that powers multiple value formatters.<br/>
    /// Takes in the result values from the Play Report, and outputs:
    /// <br/>
    /// a formatted string,
    /// <br/>
    /// a signal that nothing was available to handle it,
    /// <br/>
    /// OR a signal to reset the value that the caller is using the <see cref="Analyzer"/> for. 
    /// </summary>
    public delegate FormattedValue MultiValueFormatter(MultiValue value);

    /// <summary>
    /// The delegate type that powers multiple value formatters.
    /// The dictionary passed to this delegate is sparsely populated;
    /// that is, not every key specified in the Play Report needs to match for this to be used.<br/>
    /// Takes in the result values from the Play Report, and outputs:
    /// <br/>
    /// a formatted string,
    /// <br/>
    /// a signal that nothing was available to handle it,
    /// <br/>
    /// OR a signal to reset the value that the caller is using the <see cref="Analyzer"/> for. 
    /// </summary>
    public delegate FormattedValue SparseMultiValueFormatter(SparseMultiValue value);
}
