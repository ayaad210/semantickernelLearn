using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace AIKnowledgeAssistant.Plugins;

// Milestone 6: a tiny tool that the model uses AFTER it has found a number in the documents.
//
// Why a tool for simple subtraction? Models can make arithmetic mistakes.
// With a tool, the math is done by our C# code, so the answer is always exact.
// It also gives the model a SECOND step to take, which is what makes the flow multi-step.
public sealed class CalculatorPlugin
{
    [KernelFunction("CalculateRemainingDays")]
    [Description("Calculate remaining days as totalDays minus usedDays.")]
    public int CalculateRemainingDays(
        [Description("Total days available, for example the annual leave balance from the policy")] int totalDays,
        [Description("Days already used")] int usedDays) =>
        totalDays - usedDays;
}
