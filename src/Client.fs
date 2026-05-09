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
open WebSharper.JavaScript

[<JavaScript>]
module Client =

    // -----------------------------
    // Parsing and conversion helpers
    // -----------------------------

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

    let private categoryToString category =
        match category with
        | ETF -> "ETF"
        | Stock -> "Stock"
        | Bond -> "Bond"
        | Cash -> "Cash"
        | Crypto -> "Crypto"

    // -----------------------------
    // Browser integration helpers
    // -----------------------------
    // These inline JavaScript helpers are used for browser-only features
    // that are not part of the pure F# domain model.

    [<Inline("(function(filename, content) { var blob = new Blob([content], { type: 'application/json' }); var url = URL.createObjectURL(blob); var a = document.createElement('a'); a.href = url; a.download = filename; document.body.appendChild(a); a.click(); document.body.removeChild(a); URL.revokeObjectURL(url); })($filename, $content)")>]
    let private downloadJsonFile (filename: string) (content: string) : unit =
        X<unit>

    [<Inline("window.localStorage.setItem($key, $value)")>]
    let private saveTextToLocalStorage (key: string) (value: string) : unit =
        X<unit>

    [<Inline("window.localStorage.getItem($key) || ''")>]
    let private loadTextFromLocalStorage (key: string) : string =
        X<string>

    [<Inline("window.localStorage.removeItem($key)")>]
    let private removeTextFromLocalStorage (key: string) : unit =
        X<unit>

    // -----------------------------
    // JSON import accessors
    // -----------------------------
    // WebSharper compiles this code to JavaScript, so these small inline
    // helpers safely read fields from parsed JSON objects.

    [<Inline("JSON.parse($json)")>]
    let private parseJsonObject (json: string) : obj =
        X<obj>

    [<Inline("($data.assets || [])")>]
    let private getJsonAssets (data: obj) : obj[] =
        X<obj[]>

    [<Inline("($data.portfolios || [])")>]
    let private getJsonPortfolios (data: obj) : obj[] =
        X<obj[]>

    [<Inline("($asset.id || '')")>]
    let private getAssetId (asset: obj) : string =
        X<string>

    [<Inline("($asset.name || '')")>]
    let private getAssetName (asset: obj) : string =
        X<string>

    [<Inline("($asset.symbol || '')")>]
    let private getAssetSymbol (asset: obj) : string =
        X<string>

    [<Inline("($asset.category || 'ETF')")>]
    let private getAssetCategory (asset: obj) : string =
        X<string>

    [<Inline("Number($asset.currentPrice || 0)")>]
    let private getAssetCurrentPrice (asset: obj) : float =
        X<float>

    [<Inline("Number($asset.expectedAnnualReturn || 0)")>]
    let private getAssetExpectedReturn (asset: obj) : float =
        X<float>

    [<Inline("Number($asset.annualVolatility || 0)")>]
    let private getAssetVolatility (asset: obj) : float =
        X<float>

    [<Inline("Number($asset.annualFee || 0)")>]
    let private getAssetFee (asset: obj) : float =
        X<float>

    [<Inline("Number($asset.liquidityScore || 0)")>]
    let private getAssetLiquidity (asset: obj) : float =
        X<float>

    [<Inline("Number($asset.diversificationScore || 0)")>]
    let private getAssetDiversification (asset: obj) : float =
        X<float>

    [<Inline("Number($asset.riskScore || 0)")>]
    let private getAssetRisk (asset: obj) : float =
        X<float>

    [<Inline("($portfolio.id || '')")>]
    let private getPortfolioId (portfolio: obj) : string =
        X<string>

    [<Inline("($portfolio.name || '')")>]
    let private getPortfolioName (portfolio: obj) : string =
        X<string>

    [<Inline("($portfolio.allocations || [])")>]
    let private getPortfolioAllocations (portfolio: obj) : obj[] =
        X<obj[]>

    [<Inline("($allocation.assetId || '')")>]
    let private getAllocationAssetId (allocation: obj) : string =
        X<string>

    [<Inline("Number($allocation.percentage || 0)")>]
    let private getAllocationPercentage (allocation: obj) : float =
        X<float>

    // -----------------------------
    // Decision and validation helpers
    // -----------------------------

    let private optionValueOrEmpty (value: string) =
        if value.Trim() = "" then None else Some value

    let private isApproximately100 (value: float) =
        abs (value - 100.0) < 0.01

    let private buildCriteria (r: float) (rk: float) (f: float) (l: float) (d: float) : Criterion list =
        [
            { Id = "return"; Name = "Expected Return"; Kind = Benefit; Weight = r }
            { Id = "risk"; Name = "Risk"; Kind = Cost; Weight = rk }
            { Id = "fee"; Name = "Fee"; Kind = Cost; Weight = f }
            { Id = "liquidity"; Name = "Liquidity"; Kind = Benefit; Weight = l }
            { Id = "diversification"; Name = "Diversification"; Kind = Benefit; Weight = d }
        ]

    // -----------------------------
    // Reusable UI fragments
    // -----------------------------
    // Small view helpers keep the main page layout shorter and easier to read.

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

    // Render a simple custom growth chart from simulation points.
    // The chart uses positioned HTML elements instead of an external chart library.

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

    // -----------------------------
    // Form input helpers
    // -----------------------------

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

    // Asset selection helper for portfolio allocation inputs.
    // The visible buttons fill the underlying AssetId field.

    let private assetSelectorField (labelText: string) (state: Var<string>) (assetsView: View<Asset list>) =
        div [ attr.``class`` "weight-field" ] [
            label [ attr.``class`` "weight-label" ] [ text labelText ]

            Doc.InputType.Text [
                attr.``class`` "weight-input"
                attr.placeholder "Paste asset ID or click below"
            ] state

            Doc.BindView
                (fun (assets: Asset list) ->
                    div [ attr.style "display:flex; flex-direction:column; gap:6px; margin-top:8px;" ] [
                        for (asset: Asset) in assets do
                            button [
                                attr.``class`` "preset-button"
                                attr.style "padding:6px 10px; font-size:0.8rem; text-align:left;"
                                on.click (fun _ _ ->
                                    state.Set asset.Id
                                )
                            ] [
                                text (sprintf "%s (%s)" asset.Name asset.Symbol)
                            ]
                    ]
                )
                assetsView
        ]

    // -----------------------------
    // JSON export helpers
    // -----------------------------

    let private escapeJson (value: string) =
        value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")

    let private assetToJson (asset: Asset) =
        sprintf
            """{
    "id": "%s",
    "name": "%s",
    "symbol": "%s",
    "category": "%s",
    "currentPrice": %.2f,
    "expectedAnnualReturn": %.2f,
    "annualVolatility": %.2f,
    "annualFee": %.2f,
    "liquidityScore": %.2f,
    "diversificationScore": %.2f,
    "riskScore": %.2f
  }"""
            (escapeJson asset.Id)
            (escapeJson asset.Name)
            (escapeJson asset.Symbol)
            (categoryToString asset.Category)
            asset.CurrentPrice
            asset.ExpectedAnnualReturn
            asset.AnnualVolatility
            asset.AnnualFee
            asset.LiquidityScore
            asset.DiversificationScore
            asset.RiskScore

    let private allocationToJson (allocation: PortfolioAllocation) =
        sprintf
            """    {
      "assetId": "%s",
      "percentage": %.2f
    }"""
            (escapeJson allocation.AssetId)
            allocation.Percentage

    let private portfolioToJson (portfolio: Portfolio) =
        let allocationsJson =
            portfolio.Allocations
            |> List.map allocationToJson
            |> String.concat ",\n"

        sprintf
            """{
    "id": "%s",
    "name": "%s",
    "allocations": [
%s
    ]
  }"""
            (escapeJson portfolio.Id)
            (escapeJson portfolio.Name)
            allocationsJson

    let private exportDataToJson (assets: Asset list) (portfolios: Portfolio list) =
        let assetsJson =
            assets
            |> List.map assetToJson
            |> String.concat ",\n"

        let portfoliosJson =
            portfolios
            |> List.map portfolioToJson
            |> String.concat ",\n"

        sprintf
            """{
  "assets": [
%s
  ],
  "portfolios": [
%s
  ]
}"""
            assetsJson
            portfoliosJson

    // -----------------------------
    // Table row helpers
    // -----------------------------

    let private assetRow (removeAsset: string -> unit) (asset: Asset) =
        tr [] [
            td [] [ text asset.Id ]
            td [] [ text asset.Name ]
            td [] [ text asset.Symbol ]
            td [] [ text (categoryToString asset.Category) ]
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

    // -----------------------------
    // JSON import mapping helpers
    // -----------------------------
    // Convert parsed JavaScript objects back into strongly typed F# domain records.

    let private jsonAssetToAsset (asset: obj) : Asset =
        {
            Id = getAssetId asset
            Name = getAssetName asset
            Symbol = getAssetSymbol asset
            Category = categoryFromString (getAssetCategory asset)
            CurrentPrice = getAssetCurrentPrice asset
            ExpectedAnnualReturn = getAssetExpectedReturn asset
            AnnualVolatility = getAssetVolatility asset
            AnnualFee = getAssetFee asset
            LiquidityScore = getAssetLiquidity asset
            DiversificationScore = getAssetDiversification asset
            RiskScore = getAssetRisk asset
        }

    let private jsonAllocationToAllocation (allocation: obj) : PortfolioAllocation =
        {
            AssetId = getAllocationAssetId allocation
            Percentage = getAllocationPercentage allocation
        }

    let private jsonPortfolioToPortfolio (portfolio: obj) : Portfolio =
        {
            Id = getPortfolioId portfolio
            Name = getPortfolioName portfolio
            Allocations =
                getPortfolioAllocations portfolio
                |> Array.toList
                |> List.map jsonAllocationToAllocation
                |> List.filter (fun a -> a.AssetId.Trim() <> "" && a.Percentage > 0.0)
        }

    let private tryImportDataFromJson (json: string) =
        try
            let data = parseJsonObject json

            let assets =
                getJsonAssets data
                |> Array.toList
                |> List.map jsonAssetToAsset
                |> List.filter (fun a -> a.Id.Trim() <> "" && a.Name.Trim() <> "")

            let portfolios =
                getJsonPortfolios data
                |> Array.toList
                |> List.map jsonPortfolioToPortfolio
                |> List.filter (fun p -> p.Id.Trim() <> "" && p.Name.Trim() <> "")

            Some (assets, portfolios)
        with _ ->
            None

    let private portfolioRow (removePortfolio: string -> unit) (portfolio: Portfolio) =
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
            td [] [
                button [
                    attr.``class`` "delete-button"
                    on.click (fun _ _ -> removePortfolio portfolio.Id)
                ] [
                    text "Delete"
                ]
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

        // -----------------------------
        // Main reactive application state
        // -----------------------------
        // Var values represent mutable client-side state.
        // Views derived from these Vars update the UI automatically.

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
        // Asset editor state
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
        // Portfolio editor state
        let portfolioNameText = Var.Create ""
        let allocationAsset1Text = Var.Create ""
        let allocationPercent1Text = Var.Create "0"
        let allocationAsset2Text = Var.Create ""
        let allocationPercent2Text = Var.Create "0"
        let allocationAsset3Text = Var.Create ""
        let allocationPercent3Text = Var.Create "0"
        // JSON export/import and browser storage state
        let exportedJsonText = Var.Create ""
        let storageStatusText = Var.Create ""

        // Derived view used to validate whether portfolio allocations add up to 100%
        let allocationTotalView : View<float> =
            View.Map2
                (fun left p3 ->
                    let (p1, p2) = left
                    parseOrZero p1 + parseOrZero p2 + parseOrZero p3
                )
                (View.Map2
                    (fun p1 p2 -> (p1, p2))
                    allocationPercent1Text.View
                    allocationPercent2Text.View)
                allocationPercent3Text.View

        // -----------------------------
        // Form reset helpers
        // -----------------------------

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

        // -----------------------------
        // Asset and portfolio mutation logic
        // -----------------------------

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

            // Removing an asset also removes invalid allocations that reference it
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

            let totalPercentage =
                newPortfolio.Allocations
                |> List.sumBy (fun a -> a.Percentage)

            if newPortfolio.Name.Trim() <> "" &&
               not newPortfolio.Allocations.IsEmpty &&
               allAllocationsValid &&
               isApproximately100 totalPercentage then
                portfoliosState.Set (portfoliosState.Value @ [ newPortfolio ])
                clearPortfolioForm ()

        let removePortfolio portfolioId =
            portfoliosState.Set (
                portfoliosState.Value
                |> List.filter (fun p -> p.Id <> portfolioId)
            )

        // -----------------------------
        // JSON export/import and browser storage actions
        // -----------------------------

        let exportCurrentData () =
            let json =
                exportDataToJson assetsState.Value portfoliosState.Value

            exportedJsonText.Set json

        let saveCurrentDataToBrowser () =
            let json =
                exportDataToJson assetsState.Value portfoliosState.Value

            exportedJsonText.Set json
            saveTextToLocalStorage "portfolioPilotData" json
            storageStatusText.Set "Current data saved in this browser."

        let loadSavedDataFromBrowser () =
            let savedJson =
                loadTextFromLocalStorage "portfolioPilotData"

            if savedJson.Trim() = "" then
                storageStatusText.Set "No saved data found in this browser."
            else
                exportedJsonText.Set savedJson
                storageStatusText.Set "Saved JSON loaded into the export box."

        let clearSavedDataFromBrowser () =
            removeTextFromLocalStorage "portfolioPilotData"
            storageStatusText.Set "Saved browser data cleared."

        let importJsonFromBox () =
            match tryImportDataFromJson exportedJsonText.Value with
            | Some (assets, portfolios) when not assets.IsEmpty ->
                assetsState.Set assets
                portfoliosState.Set portfolios
                storageStatusText.Set (
                    sprintf "Imported %d assets and %d portfolios from JSON."
                        (List.length assets)
                        (List.length portfolios)
                )

            | Some _ ->
                storageStatusText.Set "JSON is valid, but it does not contain valid assets."

            | None ->
                storageStatusText.Set "Invalid JSON. Import failed."

        // -----------------------------
        // Reactive derived views
        // -----------------------------
        // These views recalculate automatically whenever the underlying Vars change.

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

        // -----------------------------
        // Page layout
        // -----------------------------

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

                // Recommended result based on the current weighted ranking
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

                // Manual asset creation section
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

                // Current asset table
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

                // Portfolio creation section with allocation validation
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
                            assetSelectorField "Asset" allocationAsset1Text assetsState.View
                            formField "Percentage" allocationPercent1Text
                        ]

                        div [ attr.``class`` "allocation-card" ] [
                            h4 [] [ text "Asset 2" ]
                            assetSelectorField "Asset" allocationAsset2Text assetsState.View
                            formField "Percentage" allocationPercent2Text
                        ]

                        div [ attr.``class`` "allocation-card" ] [
                            h4 [] [ text "Asset 3" ]
                            assetSelectorField "Asset" allocationAsset3Text assetsState.View
                            formField "Percentage" allocationPercent3Text
                        ]
                    ]

                    Doc.BindView
                        (fun total ->
                            let isValid = isApproximately100 total

                            div [
                                attr.``class`` (
                                    if isValid then
                                        "allocation-status allocation-status-ok"
                                    else
                                        "allocation-status allocation-status-error"
                                )
                            ] [
                                if isValid then
                                    text (sprintf "Allocation total: %.0f%% — valid" total)
                                else
                                    text (sprintf "Allocation total: %.0f%% — must be exactly 100%%" total)
                            ]
                        )
                        allocationTotalView

                    div [ attr.``class`` "editor-actions" ] [
                        button [
                            attr.``class`` "preset-button"
                            on.click (fun _ _ -> addPortfolio ())
                        ] [
                            text "Add portfolio"
                        ]
                    ]
                ]

                // Portfolio table with delete support
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
                                        th [] [ text "Action" ]
                                    ]
                                ]
                                tbody [] [
                                    for portfolio in portfolios do
                                        portfolioRow removePortfolio portfolio
                                ]
                            ]
                        )
                        portfoliosState.View
                ]

                // Detailed portfolio cards
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

                // Visual comparison of weighted scores
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

                // Ranked portfolio cards with score explanations
                h2 [] [ text "Portfolio ranking" ]

                Doc.BindView
                    (fun ranking ->
                        div [ attr.``class`` "portfolio-grid" ] [
                            for i, portfolioScore in ranking |> List.indexed do
                                rankingCard (i + 1) portfolioScore
                        ]
                    )
                    rankingView

                // Simulation input controls
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

                // Time-based growth visualization
                h2 [] [ text "Growth chart" ]

                div [ attr.``class`` "summary-box" ] [
                    Doc.BindView
                        (fun results -> growthChart results)
                        simulationView
                ]

                // Final simulated value comparison
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

                // Numeric simulation result cards
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

                // JSON export, import and browser persistence controls
                h2 [] [ text "Data export" ]

                div [ attr.``class`` "summary-box weights-panel" ] [
                    p [ attr.``class`` "panel-description" ] [
                        text "Export the current assets and portfolios as JSON. This can later be used for import or backup."
                    ]

                    div [ attr.``class`` "editor-actions" ] [
                        button [
                            attr.``class`` "preset-button"
                            on.click (fun _ _ ->
                                let json = exportDataToJson assetsState.Value portfoliosState.Value
                                exportedJsonText.Set json
                                downloadJsonFile "portfolio-data.json" json
                            )
                        ] [
                            text "Export current data as JSON"
                        ]

                        button [
                            attr.``class`` "preset-button"
                            on.click (fun _ _ -> saveCurrentDataToBrowser ())
                        ] [
                            text "Save in browser"
                        ]

                        button [
                            attr.``class`` "preset-button"
                            on.click (fun _ _ -> loadSavedDataFromBrowser ())
                        ] [
                            text "Load saved JSON"
                        ]

                        button [
                            attr.``class`` "preset-button"
                            on.click (fun _ _ -> importJsonFromBox ())
                        ] [
                            text "Import JSON from text"
                        ]

                        button [
                            attr.``class`` "delete-button"
                            on.click (fun _ _ -> clearSavedDataFromBrowser ())
                        ] [
                            text "Clear saved data"
                        ]
                    ]

                    Doc.BindView
                        (fun message ->
                            p [ attr.``class`` "storage-status" ] [
                                text message
                            ]
                        )
                        storageStatusText.View

                    Doc.InputArea [
                        attr.``class`` "json-output"
                        attr.rows "16"
                        attr.placeholder "Exported JSON will appear here..."
                    ] exportedJsonText
                ]
            ]

        Doc.RunById "main" content