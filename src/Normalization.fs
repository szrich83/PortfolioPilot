namespace PortfolioPilot

open WebSharper

[<JavaScript>]
module Normalization =

    // Normalize a benefit criterion where larger values are preferred.
    // Result is scaled between 0 and 1.
    let normalizeBenefit (minVal: float) (maxVal: float) (x: float) =
        if maxVal = minVal then
            1.0
        else
            (x - minVal) / (maxVal - minVal)

    // Normalize a cost criterion where smaller values are preferred.
    // Result is scaled between 0 and 1.
    let normalizeCost (minVal: float) (maxVal: float) (x: float) =
        if maxVal = minVal then
            1.0
        else
            (maxVal - x) / (maxVal - minVal)