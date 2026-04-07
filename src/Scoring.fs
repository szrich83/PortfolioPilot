namespace PortfolioPilot

open WebSharper
open PortfolioPilot.Normalization

[<JavaScript>]
module Scoring =

    let normalizeWeights (criteria: Criterion list) =
        let total = criteria |> List.sumBy (fun c -> c.Weight)

        if total = 0.0 then
            criteria
        else
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

    let private getMinMax (criterionId: string) (items: (Portfolio * PortfolioMetric) list) =
        let values =
            items
            |> List.map (fun (_, metric) -> getMetricValue metric criterionId)

        (List.min values, List.max values)

    let scorePortfolios (portfolios: (Portfolio * PortfolioMetric) list) (criteria: Criterion list) : PortfolioScore list =
        let normalizedCriteria = normalizeWeights criteria

        portfolios
        |> List.map (fun (portfolio, metrics) ->
            let criterionScores =
                normalizedCriteria
                |> List.map (fun criterion ->
                    let rawValue = getMetricValue metrics criterion.Id
                    let (minVal, maxVal) = getMinMax criterion.Id portfolios

                    let normalizedValue =
                        match criterion.Kind with
                        | Benefit -> normalizeBenefit minVal maxVal rawValue
                        | Cost -> normalizeCost minVal maxVal rawValue

                    let weightedScore = normalizedValue * criterion.Weight

                    {
                        CriterionId = criterion.Id
                        CriterionName = criterion.Name
                        RawValue = rawValue
                        NormalizedValue = normalizedValue
                        WeightedScore = weightedScore
                    }
                )

            let totalScore =
                criterionScores
                |> List.sumBy (fun s -> s.WeightedScore)

            {
                PortfolioId = portfolio.Id
                PortfolioName = portfolio.Name
                TotalScore = totalScore
                CriteriaScores = criterionScores
                Metrics = metrics
            }
        )
        |> List.sortByDescending (fun p -> p.TotalScore)