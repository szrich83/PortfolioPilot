namespace PortfolioPilot

open WebSharper

[<JavaScript>]
module Explanation =

    let private topPositiveCriteria (scores: CriterionScore list) =
        scores
        |> List.sortByDescending (fun s -> s.WeightedScore)
        |> List.truncate 2

    let private weakestCriteria (scores: CriterionScore list) =
        scores
        |> List.sortBy (fun s -> s.WeightedScore)
        |> List.truncate 1

    let buildPortfolioExplanation (portfolioScore: PortfolioScore) =
        let strengths = topPositiveCriteria portfolioScore.CriteriaScores
        let weaknesses = weakestCriteria portfolioScore.CriteriaScores

        let strengthsText =
            strengths
            |> List.map (fun s -> s.CriterionName.ToLower())
            |> String.concat " and "

        let weaknessesText =
            weaknesses
            |> List.map (fun s -> s.CriterionName.ToLower())
            |> String.concat ", "

        sprintf
            "%s ranks with a total score of %.3f. Its strongest areas are %s. The weakest contributing factor is %s."
            portfolioScore.PortfolioName
            portfolioScore.TotalScore
            strengthsText
            weaknessesText

    let buildWinnerExplanation (ranking: PortfolioScore list) =
        match ranking with
        | [] ->
            "No portfolio data is available."
        | winner :: _ ->
            let topTwo =
                winner.CriteriaScores
                |> List.sortByDescending (fun s -> s.WeightedScore)
                |> List.truncate 2
                |> List.map (fun s -> s.CriterionName.ToLower())
                |> String.concat " and "

            sprintf
                "The recommended portfolio is %s with a total score of %.3f. It performs best overall mainly because of its strong %s profile under the current weighting."
                winner.PortfolioName
                winner.TotalScore
                topTwo