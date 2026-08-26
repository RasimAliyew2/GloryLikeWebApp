(() => {
    "use strict";

    const dashboard = document.querySelector("[data-reports-dashboard]");
    if (!dashboard) return;

    const tabButtons = Array.from(
        dashboard.querySelectorAll("[data-reports-tab]"));
    const tabPanels = Array.from(
        dashboard.querySelectorAll("[data-reports-panel]"));
    const activeTabInput = document.getElementById("reportsActiveTabInput");
    const chartContainer = document.getElementById("reportsActivityChart");
    const chartDataElement = document.getElementById("reportsChartData");
    const svgNamespace = "http://www.w3.org/2000/svg";

    let chartData = { labels: [], applications: [], hired: [] };
    if (chartDataElement?.textContent) {
        try {
            chartData = JSON.parse(chartDataElement.textContent);
        } catch {
            chartData = { labels: [], applications: [], hired: [] };
        }
    }

    const createSvgElement = (name, attributes = {}) => {
        const element = document.createElementNS(svgNamespace, name);
        Object.entries(attributes).forEach(([key, value]) => {
            element.setAttribute(key, String(value));
        });
        return element;
    };

    const appendText = (svg, value, x, y, className, anchor = "middle") => {
        const text = createSvgElement("text", {
            x,
            y,
            class: className,
            "text-anchor": anchor
        });
        text.textContent = value;
        svg.appendChild(text);
    };

    const buildPoints = (values, width, height, padding, maximum) => {
        const chartWidth = width - padding.left - padding.right;
        const chartHeight = height - padding.top - padding.bottom;
        const denominator = Math.max(1, values.length - 1);

        return values.map((rawValue, index) => {
            const value = Number(rawValue) || 0;
            return {
                x: padding.left + chartWidth * index / denominator,
                y: padding.top + chartHeight * (1 - value / maximum),
                value
            };
        });
    };

    const drawSeries = (svg, points, className, areaHeight) => {
        if (points.length === 0) return;

        if (className === "applications") {
            const areaPath = [
                `M ${points[0].x} ${areaHeight}`,
                ...points.map(point => `L ${point.x} ${point.y}`),
                `L ${points.at(-1).x} ${areaHeight}`,
                "Z"
            ].join(" ");
            svg.appendChild(createSvgElement("path", {
                d: areaPath,
                class: "reports-chart-area"
            }));
        }

        const polyline = createSvgElement("polyline", {
            points: points.map(point => `${point.x},${point.y}`).join(" "),
            class: `reports-chart-line ${className}`
        });
        svg.appendChild(polyline);

        points.forEach(point => {
            svg.appendChild(createSvgElement("circle", {
                cx: point.x,
                cy: point.y,
                r: 3.5,
                class: `reports-chart-point ${className}`
            }));
        });
    };

    const renderChart = () => {
        if (!chartContainer || chartContainer.closest("[hidden]")) return;

        const svg = chartContainer.querySelector("svg");
        const empty = chartContainer.querySelector(".reports-chart-empty");
        if (!svg) return;

        const labels = Array.isArray(chartData.labels) ? chartData.labels : [];
        const applications = Array.isArray(chartData.applications)
            ? chartData.applications.map(Number)
            : [];
        const hired = Array.isArray(chartData.hired)
            ? chartData.hired.map(Number)
            : [];

        svg.replaceChildren();
        if (labels.length === 0) {
            svg.hidden = true;
            if (empty) empty.hidden = false;
            return;
        }

        svg.hidden = false;
        if (empty) empty.hidden = true;

        const width = 1120;
        const height = 330;
        const padding = { top: 24, right: 24, bottom: 58, left: 54 };
        const rawMaximum = Math.max(0, ...applications, ...hired);
        const maximum = Math.max(5, Math.ceil(rawMaximum / 5) * 5);
        const chartBottom = height - padding.bottom;
        const chartRight = width - padding.right;
        svg.setAttribute("viewBox", `0 0 ${width} ${height}`);

        for (let index = 0; index <= 5; index += 1) {
            const y = padding.top
                + (chartBottom - padding.top) * index / 5;
            const value = Math.round(maximum * (1 - index / 5));
            svg.appendChild(createSvgElement("line", {
                x1: padding.left,
                y1: y,
                x2: chartRight,
                y2: y,
                class: "reports-chart-grid"
            }));
            appendText(
                svg,
                value,
                padding.left - 12,
                y + 4,
                "reports-chart-axis-label",
                "end");
        }

        const appPoints = buildPoints(
            applications,
            width,
            height,
            padding,
            maximum);
        const hiredPoints = buildPoints(
            hired,
            width,
            height,
            padding,
            maximum);
        const labelStep = Math.max(1, Math.ceil(labels.length / 9));

        labels.forEach((label, index) => {
            if (index % labelStep !== 0 && index !== labels.length - 1) return;
            const point = appPoints[index]
                ?? buildPoints([0], width, height, padding, maximum)[0];
            appendText(
                svg,
                label,
                point.x,
                height - 24,
                "reports-chart-axis-label");
        });

        drawSeries(svg, appPoints, "applications", chartBottom);
        drawSeries(svg, hiredPoints, "hired", chartBottom);
    };

    const activateTab = tabId => {
        tabButtons.forEach(button => {
            const isActive = button.dataset.reportsTab === tabId;
            button.classList.toggle("active", isActive);
            button.setAttribute("aria-selected", String(isActive));
        });
        tabPanels.forEach(panel => {
            panel.hidden = panel.dataset.reportsPanel !== tabId;
        });
        if (activeTabInput) activeTabInput.value = tabId;

        const url = new URL(window.location.href);
        url.searchParams.set("Tab", tabId);
        window.history.replaceState({}, "", url);

        if (tabId === "overview") window.requestAnimationFrame(renderChart);
    };

    tabButtons.forEach(button => {
        button.addEventListener("click", () => {
            activateTab(button.dataset.reportsTab ?? "overview");
        });
    });

    const escapeXml = value => String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&apos;");

    const excelCell = value => {
        const normalized = String(value ?? "").trim();
        const numeric = /^-?\d+(?:[.,]\d+)?$/.test(normalized);
        const type = numeric ? "Number" : "String";
        const cellValue = numeric ? normalized.replace(",", ".") : normalized;
        return `<Cell><Data ss:Type="${type}">${escapeXml(cellValue)}</Data></Cell>`;
    };

    const exportToExcel = () => {
        const activePanel = tabPanels.find(panel => !panel.hidden);
        if (!activePanel) return;

        const tables = Array.from(
            activePanel.querySelectorAll("[data-export-table]"));
        const rows = [];
        tables.forEach(table => {
            const title = table.dataset.exportTable ?? "Report";
            rows.push(`<Row>${excelCell(title)}</Row>`);
            table.querySelectorAll("tr").forEach(row => {
                const cells = Array.from(row.querySelectorAll("th, td"))
                    .map(cell => excelCell(cell.textContent));
                rows.push(`<Row>${cells.join("")}</Row>`);
            });
            rows.push("<Row></Row>");
        });

        const workbook = `<?xml version="1.0"?>
<?mso-application progid="Excel.Sheet"?>
<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"
 xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">
 <Worksheet ss:Name="BothFind report"><Table>${rows.join("")}</Table></Worksheet>
</Workbook>`;
        const blob = new Blob([workbook], {
            type: "application/vnd.ms-excel;charset=utf-8"
        });
        const link = document.createElement("a");
        const dateFrom = document.querySelector("input[name='DateFrom']")?.value
            ?? "from";
        const dateTo = document.querySelector("input[name='DateTo']")?.value
            ?? "to";
        const objectUrl = URL.createObjectURL(blob);
        link.href = objectUrl;
        link.download = `BothFind-report-${dateFrom}-${dateTo}.xls`;
        document.body.appendChild(link);
        link.click();
        link.remove();
        window.setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
    };

    dashboard.querySelector("[data-reports-export='excel']")
        ?.addEventListener("click", exportToExcel);
    dashboard.querySelector("[data-reports-export='pdf']")
        ?.addEventListener("click", () => window.print());

    let resizeFrame = 0;
    window.addEventListener("resize", () => {
        window.cancelAnimationFrame(resizeFrame);
        resizeFrame = window.requestAnimationFrame(renderChart);
    });

    renderChart();
})();
