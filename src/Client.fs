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

    let private parseOrZero (textValue: string) =
        match System.Double.TryParse(textValue) with
        | true, value -> value
        | _ -> 0.0

    let private categoryFromString (value: string) =
        match value.Trim().ToLower() with
        | "stock" -> Stock
        | "bond" -> Bond
        | "cash" -> Cash
        | "crypto" -> Crypto
        | _ -> ETF

    let private optionValueOrEmpty (value: string) =
        if value.Trim() = "" then None else Some value

    let private buildCriteria (r: float) (rk: float) (f: float) (l: float) (d: float) : Criterion list =
        [
            { Id = "return"; Name = "Expected Return"; Kind = Benefit; Weight = r }
            { Id = "risk"; Name = "Risk"; Kind = Cost; Weight = rk }
            { Id = "fee"; Name = "Fee"; Kind = Cost; Weight = f }
            { Id = "liquidity"; Name = "Liquidity"; Kind = Benefit; Weight = l }
            { Id = "diversification"; Name = "Diversification"; Kind = Benefit; Weight = d }
        ]

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

    let private portfolioCard (assets: Asset list) (portfolio: Portfolio) =
        let metrics = calculatePortfolioMetrics assets portfolio
        let totalAllocation = allocationTotal portfolio
        let allocationOk = isAllocationValid portfolio

        div [ attr.``class`` "portfolio-card" ] [
            h3 [] [ text portfolio.Name ]

            p [] [ text (sprintf "Total allocation: %.0f%%" totalAllocation) ]

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
                    allocationRow assets allocation
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
                            sprintf "left: %.2f%%; bottom: %.2f%%; background:%s;"
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

    let private formField (labelText: string) (state: Var<string>) =
        div [ attr.``class`` "weight-field" ] [
            label [ attr.``class`` "weight-label" ] [ text labelText ]
            Doc.InputType.Text [
                attr.``class`` "weight-input"
            ] state
        ]

    let private assetRow (removeAsset: string -> unit) (asset: Asset) =
        tr [] [
            td [] [ text asset.Id ]
            td [] [ text asset.Name ]
            td [] [ text asset.Symbol ]
            td [] [ text (string asset.Category) ]
            td [] [ text (sprintf "%.2f" asset.CurrentPrice) ]
            td [] [ text (sprintf "%.2f%%" asset.ExpectedAnnualReturn) ]
            td [] [ text (sprintf "%.2f" asset.RiskScore) ]
            td [] [
                button [
                    attr.``class`` "delete-button"
                    on.click (fun _ _ -> removeAsset asset.Id)
                ] [
                    text "Delete"
                ]
            ]
        ]

    let private portfolioRow (portfolio: Portfolio) =
        tr [] [
            td [] [ text portfolio.Id ]
            td [] [ text portfolio.Name ]
            td [] [ text (sprintf "%.0f%%" (allocationTotal portfolio)) ]
            td [] [
                text (
                    if isAllocationValid portfolio then
                        "Valid"
                    else
                        "Invalid"
                )
            ]
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

        let assetsState = Var.Create sampleAssets
        let portfoliosState = Var.Create samplePortfolios

        let assetNameText = Var.Create ""
        let assetSymbolText = Var.Create ""
        let assetCategoryText = Var.Create "ETF"
        let assetPriceText = Var.Create "0"
        let assetReturnText = Var.Create "0"
        let assetVolatilityText = Var.Create "0"
        let assetFeeText = Var.Create "0"
        let assetLiquidityText = Var.Create "0"
        let assetDiversificationText = Var.Create "0"
        let assetRiskText = Var.Create "0"

        let portfolioNameText = Var.Create ""
        let allocationAsset1Text = Var.Create ""
        let allocationPercent1Text = Var.Create "0"
        let allocationAsset2Text = Var.Create ""
        let allocationPercent2Text = Var.Create "0"
        let allocationAsset3Text = Var.Create ""
        let allocationPercent3Text = Var.Create "0"

        let clearAssetForm () =
            assetNameText.Set ""
            assetSymbolText.Set ""
            assetCategoryText.Set "ETF"
            assetPriceText.Set "0"
            assetReturnText.Set "0"
            assetVolatilityText.Set "0"
            assetFeeText.Set "0"
            assetLiquidityText.Set "0"
            assetDiversificationText.Set "0"
            assetRiskText.Set "0"

        let clearPortfolioForm () =
            portfolioNameText.Set ""
            allocationAsset1Text.Set ""
            allocationPercent1Text.Set "0"
            allocationAsset2Text.Set ""
            allocationPercent2Text.Set "0"
            allocationAsset3Text.Set ""
            allocationPercent3Text.Set "0"

        let addAsset () =
            let newAsset : Asset =
                {
                    Id = System.Guid.NewGuid().ToString("N")
                    Name = assetNameText.Value
                    Symbol = assetSymbolText.Value
                    Category = categoryFromString assetCategoryText.Value
                    CurrentPrice = parseOrZero assetPriceText.Value
                    ExpectedAnnualReturn = parseOrZero assetReturnText.Value
                    AnnualVolatility = parseOrZero assetVolatilityText.Value
                    AnnualFee = parseOrZero assetFeeText.Value
                    LiquidityScore = parseOrZero assetLiquidityText.Value
                    DiversificationScore = parseOrZero assetDiversificationText.Value
                    RiskScore = parseOrZero assetRiskText.Value
                }

            if newAsset.Name.Trim() <> "" && newAsset.Symbol.Trim() <> "" then
                assetsState.Set (assetsState.Value @ [ newAsset ])
                clearAssetForm ()

        let removeAsset assetId =
            let updatedAssets =
                assetsState.Value
                |> List.filter (fun a -> a.Id <> assetId)

            let updatedPortfolios =
                portfoliosState.Value
                |> List.map (fun p ->
                    {
                        p with
                            Allocations =
                                p.Allocations
                                |> List.filter (fun a -> a.AssetId <> assetId)
                    })

            assetsState.Set updatedAssets
            portfoliosState.Set updatedPortfolios

        let addPortfolio () =
            let allocations : PortfolioAllocation list =
                [
                    optionValueOrEmpty allocationAsset1Text.Value
                    |> Option.map (fun assetId ->
                        {
                            AssetId = assetId
                            Percentage = parseOrZero allocationPercent1Text.Value
                        })

                    optionValueOrEmpty allocationAsset2Text.Value
                    |> Option.map (fun assetId ->
                        {
                            AssetId = assetId
                            Percentage = parseOrZero allocationPercent2Text.Value
                        })

                    optionValueOrEmpty allocationAsset3Text.Value
                    |> Option.map (fun assetId ->
                        {
                            AssetId = assetId
                            Percentage = parseOrZero allocationPercent3Text.Value
                        })
                ]
                |> List.choose id
                |> List.filter (fun a -> a.Percentage > 0.0)

            let assetIds =
                assetsState.Value |> List.map (fun a -> a.Id) |> Set.ofList

            let allAllocationsValid =
                allocations
                |> List.forall (fun a -> Set.contains a.AssetId assetIds)

            let newPortfolio : Portfolio =
                {
                    Id = System.Guid.NewGuid().ToString("N")
                    Name = portfolioNameText.Value
                    Allocations = allocations
                }

            if newPortfolio.Name.Trim() <> "" && not newPortfolio.Allocations.IsEmpty && allAllocationsValid then
                portfoliosState.Set (portfoliosState.Value @ [ newPortfolio ])
                clearPortfolioForm ()

        let weightsView : View<float * float * float * float * float> =
            View.Map2
                (fun (r, rk) (f, (l, d)) -> (r, rk, f, l, d))
                (View.Map2
                    (fun r rk -> (parseWeight r, parseWeight rk))
                    returnWeightText.View
                    riskWeightText.View)
                (View.Map2
                    (fun f rest -> (parseWeight f, rest))
                    feeWeightText.View
                    (View.Map2
                        (fun l d -> (parseWeight l, parseWeight d))
                        liquidityWeightText.View
                        diversificationWeightText.View))

        let portfolioMetricsView : View<(Portfolio * PortfolioMetric) list> =
            View.Map2
                (fun (assets: Asset list) (portfolios: Portfolio list) ->
                    portfolios
                    |> List.map (fun (p: Portfolio) ->
                        let metrics = calculatePortfolioMetrics assets p
                        (p, metrics)
                    ))
                assetsState.View
                portfoliosState.View

        let rankingView : View<PortfolioScore list> =
            View.Map2
                (fun (portfolioMetrics: (Portfolio * PortfolioMetric) list) (weights: float * float * float * float * float) ->
                    let (r, rk, f, l, d) = weights
                    let criteria : Criterion list = buildCriteria r rk f l d
                    scorePortfolios portfolioMetrics criteria
                )
                portfolioMetricsView
                weightsView

        let simulationInputsView : View<float * float * int> =
            View.Map2
                (fun (capital, monthly) years ->
                    (capital, monthly, years))
                (View.Map2
                    (fun capital monthly ->
                        (parseNonNegativeFloat capital, parseNonNegativeFloat monthly))
                    initialCapitalText.View
                    monthlyContributionText.View)
                (yearsText.View |> View.Map parseNonNegativeInt)

        let simulationView : View<(string * float * float * SimulationPoint list) list> =
            View.Map2
                (fun (portfolioMetrics: (Portfolio * PortfolioMetric) list) (initialCapital, monthlyContribution, years) ->
                    portfolioMetrics
                    |> List.map (fun (portfolio, metrics) ->
                        let points =
                            simulatePortfolioGrowth initialCapital monthlyContribution years metrics.ExpectedReturn

                        let finalValue = getFinalValue points
                        (portfolio.Name, metrics.ExpectedReturn, finalValue, points)
                    )
                    |> List.sortByDescending (fun (_, _, finalValue, _) -> finalValue)
                )
                portfolioMetricsView
                simulationInputsView

        let content =
            div [ attr.``class`` "page" ] [
                h1 [] [ text "PortfolioPilot" ]

                p [] [
                    text "A manual-data portfolio decision support and simulation tool."
                ]

                div [ attr.``class`` "summary-box" ] [
                    Doc.BindView
                        (fun assets ->
                            Doc.BindView
                                (fun portfolios ->
                                    div [] [
                                        p [] [ text (sprintf "Available assets: %d" (List.length assets)) ]
                                        p [] [ text (sprintf "Available portfolios: %d" (List.length portfolios)) ]
                                        p [] [ text "Decision criteria: 5" ]
                                    ]
                                )
                                portfoliosState.View
                        )
                        assetsState.View
                ]

                h2 [] [ text "Adjust weights" ]

                div [ attr.``class`` "summary-box weights-panel" ] [
                    p [ attr.``class`` "panel-description" ] [
                        text "Adjust the importance of each decision criterion. Higher values increase the influence of that factor in the final portfolio ranking."
                    ]

                    div [ attr.``class`` "preset-buttons" ] [
                        presetButton "Risk-Averse" returnWeightText riskWeightText feeWeightText liquidityWeightText diversificationWeightText "20" "40" "20" "10" "10"
                        presetButton "Balanced" returnWeightText riskWeightText feeWeightText liquidityWeightText diversificationWeightText "30" "25" "15" "10" "20"
                        presetButton "Growth-Focused" returnWeightText riskWeightText feeWeightText liquidityWeightText diversificationWeightText "45" "20" "10" "5" "20"
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
                            | _ :: _ -> p [] [ text (buildWinnerExplanation ranking) ]
                            | [] -> p [] [ text "No portfolio data is available." ]
                        )
                        rankingView
                ]

                h2 [] [ text "Asset editor" ]

                div [ attr.``class`` "summary-box weights-panel" ] [
                    p [ attr.``class`` "panel-description" ] [
                        text "Add custom financial assets manually."
                    ]

                    div [ attr.``class`` "editor-grid" ] [
                        formField "Name" assetNameText
                        formField "Symbol" assetSymbolText
                        formField "Category (ETF / Stock / Bond / Cash / Crypto)" assetCategoryText
                        formField "Current Price" assetPriceText
                        formField "Expected Annual Return" assetReturnText
                        formField "Annual Volatility" assetVolatilityText
                        formField "Annual Fee" assetFeeText
                        formField "Liquidity Score" assetLiquidityText
                        formField "Diversification Score" assetDiversificationText
                        formField "Risk Score" assetRiskText
                    ]

                    div [ attr.``class`` "editor-actions" ] [
                        button [
                            attr.``class`` "preset-button"
                            on.click (fun _ _ -> addAsset ())
                        ] [
                            text "Add asset"
                        ]
                    ]
                ]

                h2 [] [ text "Available assets" ]

                div [ attr.``class`` "summary-box" ] [
                    Doc.BindView
                        (fun assets ->
                            table [ attr.``class`` "asset-table" ] [
                                thead [] [
                                    tr [] [
                                        th [] [ text "ID" ]
                                        th [] [ text "Name" ]
                                        th [] [ text "Symbol" ]
                                        th [] [ text "Category" ]
                                        th [] [ text "Price" ]
                                        th [] [ text "Return" ]
                                        th [] [ text "Risk" ]
                                        th [] [ text "Action" ]
                                    ]
                                ]
                                tbody [] [
                                    for asset in assets do
                                        assetRow removeAsset asset
                                ]
                            ]
                        )
                        assetsState.View
                ]

                h2 [] [ text "Portfolio editor" ]

                div [ attr.``class`` "summary-box weights-panel" ] [
                    p [ attr.``class`` "panel-description" ] [
                        text "Create a new portfolio using up to three asset allocations."
                    ]

                    div [ attr.``class`` "portfolio-name-field" ] [
                        label [ attr.``class`` "weight-label" ] [ text "Portfolio Name" ]
                        Doc.InputType.Text [
                            attr.``class`` "weight-input"
                            attr.placeholder "e.g. My Balanced Portfolio"
                        ] portfolioNameText
                    ]

                    div [ attr.``class`` "allocation-grid" ] [
                        div [ attr.``class`` "allocation-card" ] [
                            h4 [] [ text "Asset 1" ]
                            formField "Asset ID" allocationAsset1Text
                            formField "Percentage" allocationPercent1Text
                        ]

                        div [ attr.``class`` "allocation-card" ] [
                            h4 [] [ text "Asset 2" ]
                            formField "Asset ID" allocationAsset2Text
                            formField "Percentage" allocationPercent2Text
                        ]

                        div [ attr.``class`` "allocation-card" ] [
                            h4 [] [ text "Asset 3" ]
                            formField "Asset ID" allocationAsset3Text
                            formField "Percentage" allocationPercent3Text
                        ]
                    ]

                    div [ attr.``class`` "editor-actions" ] [
                        button [
                            attr.``class`` "preset-button"
                            on.click (fun _ _ -> addPortfolio ())
                        ] [
                            text "Add portfolio"
                        ]
                    ]
                ]

                h2 [] [ text "Available portfolios" ]

                div [ attr.``class`` "summary-box" ] [
                    Doc.BindView
                        (fun portfolios ->
                            table [ attr.``class`` "asset-table" ] [
                                thead [] [
                                    tr [] [
                                        th [] [ text "ID" ]
                                        th [] [ text "Name" ]
                                        th [] [ text "Total Allocation" ]
                                        th [] [ text "Status" ]
                                    ]
                                ]
                                tbody [] [
                                    for portfolio in portfolios do
                                        portfolioRow portfolio
                                ]
                            ]
                        )
                        portfoliosState.View
                ]

                h2 [] [ text "Current portfolios" ]

                Doc.BindView
                    (fun assets ->
                        Doc.BindView
                            (fun portfolios ->
                                div [ attr.``class`` "portfolio-grid" ] [
                                    for portfolio in portfolios do
                                        portfolioCard assets portfolio
                                ]
                            )
                            portfoliosState.View
                    )
                    assetsState.View

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
                        (fun results -> growthChart results)
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