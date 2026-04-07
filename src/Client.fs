namespace PortfolioPilot

open WebSharper
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html
open PortfolioPilot.Samples
open PortfolioPilot.PortfolioMetrics
open PortfolioPilot.Scoring
open PortfolioPilot.Explanation

[<JavaScript>]
module Client =

    let private parseWeight (textValue: string) =
        match System.Double.TryParse(textValue) with
        | true, value when value >= 0.0 -> value
        | _ -> 0.0

    let private metricRow (title: string) (value: float) (suffix: string) =
        p [ attr.``class`` "metric-row" ] [
            text (sprintf "%s: %.2f%s" title value suffix)
        ]

    let private allocationRow (assets: Asset list) (allocation: PortfolioAllocation) =
        let assetName =
            assets
            |> List.tryFind (fun a -> a.Id = allocation.AssetId)
            |> Option.map (fun a -> a.Name)
            |> Option.defaultValue allocation.AssetId

        li [] [
            text (sprintf "%s - %.0f%%" assetName allocation.Percentage)
        ]

    let private criterionScoreRow (score: CriterionScore) =
        li [] [
            text (
                sprintf
                    "%s - Raw: %.2f, Normalized: %.3f, Weighted: %.3f"
                    score.CriterionName
                    score.RawValue
                    score.NormalizedValue
                    score.WeightedScore
            )
        ]

    let private portfolioCard (portfolio: Portfolio) =
        let metrics = calculatePortfolioMetrics sampleAssets portfolio
        let totalAllocation = allocationTotal portfolio
        let allocationOk = isAllocationValid portfolio

        div [ attr.``class`` "portfolio-card" ] [
            h3 [] [ text portfolio.Name ]

            p [] [
                text (sprintf "Total allocation: %.0f%%" totalAllocation)
            ]

            p [] [
                text (
                    if allocationOk then
                        "Allocation status: valid"
                    else
                        "Allocation status: invalid"
                )
            ]

            h4 [] [ text "Allocations" ]

            ul [] [
                for allocation in portfolio.Allocations do
                    allocationRow sampleAssets allocation
            ]

            h4 [] [ text "Calculated metrics" ]

            div [ attr.``class`` "metrics-box" ] [
                metricRow "Expected return" metrics.ExpectedReturn "%"
                metricRow "Risk score" metrics.Risk ""
                metricRow "Annual fee" metrics.Fee "%"
                metricRow "Liquidity" metrics.Liquidity ""
                metricRow "Diversification" metrics.Diversification ""
            ]
        ]

    let private rankingCard (position: int) (portfolioScore: PortfolioScore) =
        div [ attr.``class`` "portfolio-card" ] [
            h3 [] [
                text (sprintf "%d. %s" position portfolioScore.PortfolioName)
            ]

            p [] [
                text (sprintf "Total score: %.3f" portfolioScore.TotalScore)
            ]

            h4 [] [ text "Score breakdown" ]

            ul [] [
                for score in portfolioScore.CriteriaScores do
                    criterionScoreRow score
            ]

            h4 [] [ text "Explanation" ]

            p [] [
                text (buildPortfolioExplanation portfolioScore)
            ]
        ]

    let private weightInput (labelText: string) (state: Var<string>) =
        div [ attr.``class`` "weight-field" ] [
            label [ attr.``class`` "weight-label" ] [ text labelText ]
            Doc.InputType.Text [
                attr.``class`` "weight-input"
                attr.``type`` "number"
                attr.min "0"
                attr.max "100"
                attr.step "1"
                attr.placeholder "0-100"
            ] state
        ]

    [<SPAEntryPoint>]
    let Main () =
        let returnWeightText = Var.Create "35"
        let riskWeightText = Var.Create "25"
        let feeWeightText = Var.Create "15"
        let liquidityWeightText = Var.Create "10"
        let diversificationWeightText = Var.Create "15"

        let portfolioMetrics =
            samplePortfolios
            |> List.map (fun p -> (p, calculatePortfolioMetrics sampleAssets p))

        let weightsView =
            View.Map2
                (fun left right -> left, right)
                (View.Map2
                    (fun r rk -> parseWeight r, parseWeight rk)
                    returnWeightText.View
                    riskWeightText.View)
                (View.Map2
                    (fun f rest -> parseWeight f, rest)
                    feeWeightText.View
                    (View.Map2
                        (fun l d -> parseWeight l, parseWeight d)
                        liquidityWeightText.View
                        diversificationWeightText.View))

        let rankingView =
            weightsView
            |> View.Map (fun ((r, rk), (f, (l, d))) ->
                let criteria =
                    [
                        { Id = "return"; Name = "Expected Return"; Kind = Benefit; Weight = r }
                        { Id = "risk"; Name = "Risk"; Kind = Cost; Weight = rk }
                        { Id = "fee"; Name = "Fee"; Kind = Cost; Weight = f }
                        { Id = "liquidity"; Name = "Liquidity"; Kind = Benefit; Weight = l }
                        { Id = "diversification"; Name = "Diversification"; Kind = Benefit; Weight = d }
                    ]

                scorePortfolios portfolioMetrics criteria
            )

        let content =
            div [ attr.``class`` "page" ] [
                h1 [] [ text "PortfolioPilot" ]

                p [] [
                    text "A manual-data portfolio decision support and simulation tool."
                ]

                div [ attr.``class`` "summary-box" ] [
                    p [] [ text (sprintf "Sample assets: %d" (List.length sampleAssets)) ]
                    p [] [ text (sprintf "Sample portfolios: %d" (List.length samplePortfolios)) ]
                    p [] [ text "Sample criteria: 5" ]
                ]

                h2 [] [ text "Adjust weights" ]

                div [ attr.``class`` "summary-box weights-panel" ] [
                    p [ attr.``class`` "panel-description" ] [
                        text "Adjust the importance of each decision criterion. Higher values increase the influence of that factor in the final portfolio ranking."
                    ]

                    div [ attr.``class`` "weights-grid" ] [
                        weightInput "Return" returnWeightText
                        weightInput "Risk" riskWeightText
                        weightInput "Fee" feeWeightText
                        weightInput "Liquidity" liquidityWeightText
                        weightInput "Diversification" diversificationWeightText
                    ]
                ]

                h2 [] [ text "Recommended result" ]

                div [ attr.``class`` "summary-box" ] [
                    Doc.BindView
                        (fun ranking ->
                            match ranking with
                            | _ :: _ ->
                                p [] [ text (buildWinnerExplanation ranking) ]
                            | [] ->
                                p [] [ text "No portfolio data is available." ]
                        )
                        rankingView
                ]

                h2 [] [ text "Sample portfolios" ]

                div [ attr.``class`` "portfolio-grid" ] [
                    for portfolio in samplePortfolios do
                        portfolioCard portfolio
                ]

                h2 [] [ text "Portfolio ranking" ]

                Doc.BindView
                    (fun ranking ->
                        div [ attr.``class`` "portfolio-grid" ] [
                            for i, portfolioScore in ranking |> List.indexed do
                                rankingCard (i + 1) portfolioScore
                        ]
                    )
                    rankingView
            ]

        Doc.RunById "main" content