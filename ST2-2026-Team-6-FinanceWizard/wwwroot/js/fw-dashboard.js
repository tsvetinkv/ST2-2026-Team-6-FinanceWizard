// FinanceWizard - dashboard charts (Chart.js 4).
// Colours are read from the CSS design tokens (--fw-income-bar, --fw-expense-bar, ...) every time the
// theme changes, so the charts follow the light / dark switch without a page reload.
// The bars AND the legend squares use the same palette() values, so they can never differ.
(function () {
    'use strict';

    var holder = document.getElementById('fwDashboardData');
    if (!holder || typeof Chart === 'undefined') return;

    var data;
    try { data = JSON.parse(holder.getAttribute('data-chart')); } catch (e) { return; }

    var root = document.documentElement;
    var reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var number = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    function token(name, fallback) {
        var value = getComputedStyle(root).getPropertyValue(name).trim();
        return value || fallback;
    }
    function isDark() { return root.getAttribute('data-bs-theme') === 'dark'; }
    function money(value) {
        var s = data.symbol || '';
        return s.length === 1 ? s + number.format(value) : number.format(value) + ' ' + s;
    }
    function compact(value) {
        var abs = Math.abs(value);
        var short = abs >= 1000 ? (value / 1000).toFixed(abs >= 10000 ? 0 : 1).replace(/\.0$/, '') + 'k' : String(value);
        var s = data.symbol || '';
        return s.length === 1 ? s + short : short + ' ' + s;
    }

    // One palette for bars, legend and tooltips. Tokens first, explicit matching colours as fallback:
    //   light: income #15803d / expenses #dc2626      dark: income #4ade80 / expenses #ff8a8a
    function palette() {
        var dark = isDark();
        return {
            income: token('--fw-income-bar', dark ? '#4ade80' : '#15803d'),
            expense: token('--fw-expense-bar', dark ? '#ff8a8a' : '#dc2626'),
            text: token('--fw-text', dark ? '#e8ecf8' : '#1b2236'),
            muted: token('--fw-text-muted', dark ? '#aab4cf' : '#4a5470'),
            grid: token('--fw-border', dark ? '#37425f' : '#c9d0e0'),
            surface: token('--fw-surface', dark ? '#1a2238' : '#ffffff')
        };
    }

    // Category colour chosen by the user, or the automatic hue of its name (same idea as Hue.Of in C#)
    function sliceColor(slice) {
        if (/^#[0-9a-f]{6}$/i.test(slice.color || '')) return slice.color;
        return isDark()
            ? 'hsl(' + slice.hue + ', 62%, 62%)'
            : 'hsl(' + slice.hue + ', 55%, 42%)';
    }

    Chart.defaults.font.family = getComputedStyle(document.body).fontFamily;
    Chart.defaults.animation = reduceMotion ? false : { duration: 700 };

    var months = data.months || [];
    var categories = data.categories || [];
    var charts = [];   // { chart, apply(chart, palette) }

    // ---------------- Income vs. expenses (grouped bars) ----------------
    var monthlyCanvas = document.getElementById('fwMonthlyChart');
    if (monthlyCanvas) {
        var start = palette();

        // Colours are set when the datasets are created (not only later), so the very first paint is right
        function barDataset(label, values, color) {
            return {
                label: label,
                data: values,
                backgroundColor: color,
                hoverBackgroundColor: color,
                borderColor: color,
                borderWidth: 0,
                borderRadius: 4,
                maxBarThickness: 28
            };
        }

        var monthly = new Chart(monthlyCanvas, {
            type: 'bar',
            data: {
                labels: months.map(function (m) { return m.label; }),
                datasets: [
                    barDataset('Income', months.map(function (m) { return m.income; }), start.income),
                    barDataset('Expenses', months.map(function (m) { return m.expense; }), start.expense)
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                scales: {
                    x: { grid: { display: false }, border: {}, ticks: {} },
                    y: { beginAtZero: true, grid: {}, border: {}, ticks: { callback: function (v) { return compact(v); } } }
                },
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            boxWidth: 14,
                            boxHeight: 14,
                            // The legend squares are painted with exactly the colours of the bars
                            generateLabels: function (chart) {
                                var p = palette();
                                var items = Chart.defaults.plugins.legend.labels.generateLabels(chart);
                                items.forEach(function (item) {
                                    var color = item.datasetIndex === 0 ? p.income : p.expense;
                                    item.fillStyle = color;
                                    item.strokeStyle = color;
                                    item.lineWidth = 0;
                                    item.fontColor = p.text;
                                });
                                return items;
                            }
                        }
                    },
                    tooltip: { callbacks: { label: function (ctx) { return ctx.dataset.label + ': ' + money(ctx.parsed.y); } } }
                }
            }
        });
        charts.push({
            chart: monthly,
            apply: function (chart, p) {
                var colors = [p.income, p.expense];
                chart.data.datasets.forEach(function (ds, i) {
                    ds.backgroundColor = colors[i];
                    ds.hoverBackgroundColor = colors[i];
                    ds.borderColor = colors[i];
                });
                chart.options.scales.x.ticks.color = p.muted;
                chart.options.scales.x.border.color = p.grid;
                chart.options.scales.y.ticks.color = p.muted;
                chart.options.scales.y.grid.color = p.grid;
                chart.options.scales.y.border.color = p.grid;
            }
        });
    }

    // ---------------- Spending by category (doughnut) ----------------
    var categoryCanvas = document.getElementById('fwCategoryChart');
    if (categoryCanvas) {
        var total = categories.reduce(function (sum, s) { return sum + s.total; }, 0);
        var doughnut = new Chart(categoryCanvas, {
            type: 'doughnut',
            data: {
                labels: categories.map(function (s) { return s.name; }),
                datasets: [{
                    data: categories.map(function (s) { return s.total; }),
                    backgroundColor: categories.map(sliceColor),
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '62%',
                plugins: {
                    legend: { position: 'bottom', labels: { boxWidth: 14, boxHeight: 14 } },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                var pct = total > 0 ? Math.round(ctx.parsed / total * 100) : 0;
                                return ctx.label + ': ' + money(ctx.parsed) + ' (' + pct + '%)';
                            }
                        }
                    }
                }
            }
        });
        charts.push({
            chart: doughnut,
            apply: function (chart, p) {
                chart.data.datasets[0].backgroundColor = categories.map(sliceColor);
                chart.data.datasets[0].borderColor = p.surface;
            }
        });
    }

    // ---------------- Theme handling ----------------
    function applyTheme() {
        var p = palette();
        Chart.defaults.color = p.muted;
        Chart.defaults.borderColor = p.grid;
        charts.forEach(function (entry) {
            entry.chart.options.plugins.legend.labels.color = p.text;
            entry.apply(entry.chart, p);
            entry.chart.update('none');
        });
    }

    applyTheme();

    // The theme toggle in _Layout changes data-bs-theme on <html>; repaint the charts when it does
    new MutationObserver(applyTheme).observe(root, { attributes: true, attributeFilter: ['data-bs-theme'] });
})();