namespace PortfolioPilot

open WebSharper
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html
open PortfolioPilot.Samples
open PortfolioPilot.PortfolioMetrics
open PortfolioPilot.Scoring
open PortfolioPilot.Explanation
open PortfolioPilot.Simulation

[<JavaScript>]
module Client =

    let private parseWeight (textValue: string) =
        match System.Double.TryParse(textValue) with
        | true, value when value >= 0.0 -> value
        | _ -> 0.0

    let private parseNonNegativeFloat (textValue: string) =
        match System.Double.TryParse(textValue) with
        | true, value when value >= 0.0 -> value
        | _ -> 0.0

    let private parseNonNegativeInt (textValue: string) =
        match System.Int32.TryParse(textValue) with
        | true, value when value >= 0 -> value
        | _ -> 0

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
                sprintf "%s - Raw: %.2f, Normalized: %.3f, Weighted: %.3f"
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

            p [] [ text (sprintf "Total allocation: %.0f%%" totalAllocation) ]

            p [] [
                text (
                    if allocationOk then "Allocation status: valid"
                    else "Allocation status: invalid"
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
            h3 [] [ text (sprintf "%d. %s" position portfolioScore.PortfolioName) ]
            p [] [ text (sprintf "Total score: %.3f" portfolioScore.TotalScore) ]

            h4 [] [ text "Score breakdown" ]

            ul [] [
                for score in portfolioScore.CriteriaScores do
                    criterionScoreRow score
            ]

            h4 [] [ text "Explanation" ]
            p [] [ text (buildPortfolioExplanation portfolioScore) ]
        ]

    let private scoreBarRow (portfolioScore: PortfolioScore) =
        let widthPercent = portfolioScore.TotalScore * 100.0

        div [ attr.``class`` "score-bar-row" ] [
            div [ attr.``class`` "score-bar-header" ] [
                span [ attr.``class`` "score-bar-name" ] [ text portfolioScore.PortfolioName ]
                span [ attr.``class`` "score-bar-value" ] [ text (sprintf "%.3f" portfolioScore.TotalScore) ]
            ]
            div [ attr.``class`` "score-bar-track" ] [
                div [
                    attr.``class`` "score-bar-fill"
                    attr.style (sprintf "width: %.1f%%;" widthPercent)
                ] []
            ]
        ]

    let private growthBarRow (name: string) (finalValue: float) (maxValue: float) =
        let widthPercent =
            if maxValue <= 0.0 then 0.0
            else (finalValue / maxValue) * 100.0

        div [ attr.``class`` "score-bar-row" ] [
            div [ attr.``class`` "score-bar-header" ] [
                span [ attr.``class`` "score-bar-name" ] [ text name ]
                span [ attr.``class`` "score-bar-value" ] [ text (sprintf "%.0f" finalValue) ]
            ]
            div [ attr.``class`` "score-bar-track" ] [
                div [
                    attr.``class`` "growth-bar-fill"
                    attr.style (sprintf "width: %.1f%%;" widthPercent)
                ] []
            ]
        ]

    let private growthChart (results: (string * float * float * SimulationPoint list) list) =
        let maxMonth =
            results
            |> List.collect (fun (_, _, _, pts) -> pts)
            |> List.map (fun p -> float p.Month)
            |> List.fold max 0.0

        let maxValue =
            results
            |> List.collect (fun (_, _, _, pts) -> pts)
            |> List.map (fun p -> p.Value)
            |> List.fold max 0.0

        let chartColor index =
            match index % 5 with
            | 0 -> "#2563eb"
            | 1 -> "#16a34a"
            | 2 -> "#dc2626"
            | 3 -> "#7c3aed"
            | _ -> "#ea580c"

        let buildLine (points: SimulationPoint list) color =
            div [ attr.``class`` "chart-line" ] [
                for p in points do
                    let left =
                        if maxMonth <= 0.0 then 0.0
                        else (float p.Month / maxMonth) * 100.0

                    let bottom =
                        if maxValue <= 0.0 then 0.0
                        else (p.Value / maxValue) * 100.0

                    div [
                        attr.``class`` "chart-point"
                        attr.style (
                            sprintf
                                "left: %.2f%%; bottom: %.2f%%; background:%s;"
                                left bottom color
                        )
                    ] []
            ]

        div [ attr.``class`` "chart-card" ] [
            div [ attr.``class`` "chart-container" ] [
                for i, (_, _, _, points) in results |> List.indexed do
                    buildLine points (chartColor i)
            ]

            div [ attr.``class`` "chart-legend" ] [
                for i, (name, _, finalValue, _) in results |> List.indexed do
                    div [ attr.``class`` "chart-legend-item" ] [
                        span [
                            attr.``class`` "chart-legend-color"
                            attr.style (sprintf "background:%s;" (chartColor i))
                        ] []
                        span [] [
                            text (sprintf "%s (%.0f)" name finalValue)
                        ]
                    ]
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

    let private simulationInput (labelText: string) (state: Var<string>) =
        div [ attr.``class`` "weight-field" ] [
            label [ attr.``class`` "weight-label" ] [ text labelText ]
            Doc.InputType.Text [
                attr.``class`` "weight-input"
                attr.``type`` "number"
                attr.min "0"
                attr.step "1"
            ] state
        ]

    let private presetButton
        (buttonText: string)
        (returnWeightText: Var<string>)
        (riskWeightText: Var<string>)
        (feeWeightText: Var<string>)
        (liquidityWeightText: Var<string>)
        (diversificationWeightText: Var<string>)
        (returnValue: string)
        (riskValue: string)
        (feeValue: string)
        (liquidityValue: string)
        (diversificationValue: string) =

        button [
            attr.``class`` "preset-button"
            on.click (fun _ _ ->
                returnWeightText.Set returnValue
                riskWeightText.Set riskValue
                feeWeightText.Set feeValue
                liquidityWeightText.Set liquidityValue
                diversificationWeightText.Set diversificationValue
            )
        ] [
            text buttonText
        ]

    [<SPAEntryPoint>]
    let Main () =
        let returnWeightText = Var.Create "35"
        let riskWeightText = Var.Create "25"
        let feeWeightText = Var.Create "15"
        let liquidityWeightText = Var.Create "10"
        let diversificationWeightText = Var.Create "15"

        let initialCapitalText = Var.Create "10000"
        let monthlyContributionText = Var.Create "500"
        let yearsText = Var.Create "10"

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

        let simulationInputsView =
            View.Map2
                (fun capitalPair years ->
                    let (capital, monthly) = capitalPair
                    (capital, monthly, years)
                )
                (View.Map2
                    (fun capital monthly ->
                        parseNonNegativeFloat capital,
                        parseNonNegativeFloat monthly)
                    initialCapitalText.View
                    monthlyContributionText.View)
                (yearsText.View |> View.Map parseNonNegativeInt)

        let simulationView =
            simulationInputsView
            |> View.Map (fun (initialCapital, monthlyContribution, years) ->
                portfolioMetrics
                |> List.map (fun (portfolio, metrics) ->
                    let points =
                        simulatePortfolioGrowth initialCapital monthlyContribution years metrics.ExpectedReturn

                    let finalValue = getFinalValue points
                    (portfolio.Name, metrics.ExpectedReturn, finalValue, points)
                )
                |> List.sortByDescending (fun (_, _, finalValue, _) -> finalValue)
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

                    div [ attr.``class`` "preset-buttons" ] [
                        presetButton
                            "Risk-Averse"
                            returnWeightText
                            riskWeightText
                            feeWeightText
                            liquidityWeightText
                            diversificationWeightText
                            "20" "40" "20" "10" "10"

                        presetButton
                            "Balanced"
                            returnWeightText
                            riskWeightText
                            feeWeightText
                            liquidityWeightText
                            diversificationWeightText
                            "30" "25" "15" "10" "20"

                        presetButton
                            "Growth-Focused"
                            returnWeightText
                            riskWeightText
                            feeWeightText
                            liquidityWeightText
                            diversificationWeightText
                            "45" "20" "10" "5" "20"
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

                h2 [] [ text "Score comparison" ]

                div [ attr.``class`` "summary-box" ] [
                    Doc.BindView
                        (fun ranking ->
                            div [] [
                                for portfolioScore in ranking do
                                    scoreBarRow portfolioScore
                            ]
                        )
                        rankingView
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

                h2 [] [ text "Growth simulation" ]

                div [ attr.``class`` "summary-box weights-panel" ] [
                    p [ attr.``class`` "panel-description" ] [
                        text "Simulate long-term portfolio growth using the expected annual return of each portfolio."
                    ]

                    div [ attr.``class`` "weights-grid" ] [
                        simulationInput "Initial Capital" initialCapitalText
                        simulationInput "Monthly Contribution" monthlyContributionText
                        simulationInput "Years" yearsText
                    ]
                ]

                h2 [] [ text "Growth chart" ]

                div [ attr.``class`` "summary-box" ] [
                    Doc.BindView
                        (fun results ->
                            growthChart results
                        )
                        simulationView
                ]

                h2 [] [ text "Final value comparison" ]

                div [ attr.``class`` "summary-box" ] [
                    Doc.BindView
                        (fun results ->
                            let maxValue =
                                results
                                |> List.map (fun (_, _, finalValue, _) -> finalValue)
                                |> List.fold max 0.0

                            div [] [
                                for (name, _, finalValue, _) in results do
                                    growthBarRow name finalValue maxValue
                            ]
                        )
                        simulationView
                ]

                h2 [] [ text "Simulation results" ]

                Doc.BindView
                    (fun results ->
                        div [ attr.``class`` "portfolio-grid" ] [
                            for (name, annualReturn, finalValue, _) in results do
                                div [ attr.``class`` "portfolio-card" ] [
                                    h3 [] [ text name ]
                                    p [] [ text (sprintf "Expected annual return: %.2f%%" annualReturn) ]
                                    p [] [ text (sprintf "Final simulated value: %.0f" finalValue) ]
                                ]
                        ]
                    )
                    simulationView
            ]

        Doc.RunById "main" content