namespace PortfolioPilot

open WebSharper
open PortfolioPilot.Normalization

[<JavaScript>]
module Scoring =

    let normalizeWeights (criteria: Criterion list) =
        let total = criteria |> List.sumBy (fun c -> c.Weight)
        criteria
        |> List.map (fun c -> { c with Weight = c.Weight / total })

    let private getMetricValue (metric: PortfolioMetric) (criterionId: string) =
        match criterionId with
        | "return" -> metric.ExpectedReturn
        | "risk" -> metric.Risk
        | "fee" -> metric.Fee
        | "liquidity" -> metric.Liquidity
        | "diversification" -> metric.Diversification
        | _ -> 0.0

    let scorePortfolios (portfolios: (Portfolio * PortfolioMetric) list) (criteria: Criterion list) =
        
        let normalizedCriteria = normalizeWeights criteria

        let getMinMax criterionId =
            let values =
                portfolios
                |> List.map (fun (_, m) -> getMetricValue m criterionId)
            (List.min values, List.max values)

        portfolios
        |> List.map (fun (portfolio, metrics) ->

            let scores =
                normalizedCriteria
                |> List.map (fun c ->
                    let value = getMetricValue metrics c.Id
                    let (minVal, maxVal) = getMinMax c.Id

                    let normalized =
                        match c.Kind with
                        | Benefit -> normalizeBenefit minVal maxVal value
                        | Cost -> normalizeCost minVal maxVal value

                    (c.Id, normalized * c.Weight)
                )

            let totalScore = scores |> List.sumBy snd

            (portfolio.Name, totalScore, scores)
        )
        |> List.sortByDescending (fun (_, total, _) -> total)