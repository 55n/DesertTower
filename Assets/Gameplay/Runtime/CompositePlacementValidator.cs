using System;
using System.Collections.Generic;

/// <summary>등록 순서대로 평가하고 첫 실패에서 멈춘다. 저비용 검사를 먼저 주입한다.</summary>
public sealed class CompositePlacementValidator : IPlacementValidator
{
    private readonly IPlacementRule[] rules;
    public CompositePlacementValidator(IEnumerable<IPlacementRule> rules)
    {
        if (rules == null) throw new ArgumentNullException(nameof(rules));
        var copy = new List<IPlacementRule>();
        foreach (var rule in rules)
        {
            if (rule == null) throw new ArgumentException("Null rule.", nameof(rules));
            copy.Add(rule);
        }
        if (copy.Count == 0) throw new ArgumentException("At least one rule is required.", nameof(rules));
        this.rules = copy.ToArray();
    }

    public PlacementResult Validate(PlacementRequest request)
    {
        if (!request.IsValid)
            return PlacementResult.Denied(PlacementFailure.InvalidRequest);
        foreach (var rule in rules)
        {
            var result = rule.Evaluate(request);
            if (!result.CanPlace) return result;
        }
        return PlacementResult.Allowed();
    }
}
