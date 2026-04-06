namespace PortfolioPilot

open WebSharper

[<JavaScript>]
module Normalization =

    let normalizeBenefit (minVal: float) (maxVal: float) (x: float) =
        if maxVal = minVal then 1.0
        else (x - minVal) / (maxVal - minVal)

    let normalizeCost (minVal: float) (maxVal: float) (x: float) =
        if maxVal = minVal then 1.0
        else (maxVal - x) / (maxVal - minVal)