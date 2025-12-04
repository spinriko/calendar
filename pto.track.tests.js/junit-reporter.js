// JUnit XML Reporter for QUnit
// Exports test results in JUnit XML format for CI/CD integration

// Write user agent to DOM for debugging
var uaDiv = document.createElement("div");
uaDiv.id = "debug-user-agent";
uaDiv.style.display = "none";
uaDiv.textContent = navigator.userAgent;
document.body.appendChild(uaDiv);
(function () {
    let testResults = {
        suiteName: "JavaScript Tests",
        tests: [],
        startTime: null,
        endTime: null
    };

    QUnit.begin(function (details) {
        testResults.startTime = new Date();
        testResults.totalTests = details.totalTests;
    });

    QUnit.testDone(function (details) {
        testResults.tests.push({
            name: details.name,
            module: details.module,
            failed: details.failed,
            passed: details.passed,
            total: details.total,
            runtime: details.runtime,
            assertions: details.assertions || []
        });
    });

    QUnit.done(function (details) {
        // Write QUnit.done status to DOM for debugging
        var doneDiv = document.createElement("div");
        doneDiv.id = "debug-qunit-done";
        doneDiv.style.display = "none";
        doneDiv.textContent = "QUnit.done triggered";
        document.body.appendChild(doneDiv);
        testResults.endTime = new Date();
        testResults.runtime = details.runtime;
        testResults.passed = details.passed;
        testResults.failed = details.failed;
        testResults.total = details.total;

        const xml = generateJUnitXML(testResults);

        if (isHeadless()) {
            // For headless mode: inject XML into DOM for extraction
            const outputDiv = document.createElement("div");
            outputDiv.id = "junit-xml-output";
            outputDiv.style.display = "none";
            outputDiv.textContent = xml;
            document.body.appendChild(outputDiv);
        } else {
            // For browser mode: display download button
            displayDownloadButton(xml);
        }

        // Expose for programmatic access
        window.getTestResultsXML = function () {
            return xml;
        };
    });

    function generateJUnitXML(results) {
        const duration = (results.runtime / 1000).toFixed(3);

        let xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n";
        xml += `<testsuites name="${escapeXml(results.suiteName)}" tests="${results.total}" failures="${results.failed}" time="${duration}">\n`;

        // Group by module
        const moduleGroups = {};
        results.tests.forEach(test => {
            if (!moduleGroups[test.module]) {
                moduleGroups[test.module] = [];
            }
            moduleGroups[test.module].push(test);
        });

        // Generate testsuite for each module
        Object.keys(moduleGroups).forEach(moduleName => {
            const tests = moduleGroups[moduleName];
            const moduleFailed = tests.filter(t => t.failed > 0).length;
            const moduleTime = (tests.reduce((sum, t) => sum + t.runtime, 0) / 1000).toFixed(3);

            xml += `  <testsuite name="${escapeXml(moduleName)}" tests="${tests.length}" failures="${moduleFailed}" time="${moduleTime}">\n`;

            tests.forEach(test => {
                const testTime = (test.runtime / 1000).toFixed(3);
                xml += `    <testcase name="${escapeXml(test.name)}" classname="${escapeXml(moduleName)}" time="${testTime}"`;

                if (test.failed > 0) {
                    xml += ">\n";
                    test.assertions.forEach(assertion => {
                        if (!assertion.result) {
                            xml += `      <failure message="${escapeXml(assertion.message || "Assertion failed")}">\n`;
                            xml += `${escapeXml(assertion.message || "No details")}\n`;
                            if (assertion.expected !== undefined) {
                                xml += `Expected: ${escapeXml(String(assertion.expected))}\n`;
                            }
                            if (assertion.actual !== undefined) {
                                xml += `Actual: ${escapeXml(String(assertion.actual))}\n`;
                            }
                            xml += "      </failure>\n";
                        }
                    });
                    xml += "    </testcase>\n";
                } else {
                    xml += " />\n";
                }
            });

            xml += "  </testsuite>\n";
        });

        xml += "</testsuites>\n";
        return xml;
    }

    function escapeXml(str) {
        if (str === null || str === undefined) return "";
        return String(str)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&apos;");
    }

    function displayDownloadButton(xml) {
        const qunitHeader = document.getElementById("qunit-header");
        if (!qunitHeader) return;

        const button = document.createElement("button");
        button.textContent = "📥 Download JUnit XML Results";
        button.style.cssText = "margin-left: 20px; padding: 8px 16px; background: #4caf50; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 14px;";
        button.onclick = function () {
            downloadXML(xml);
        };

        qunitHeader.appendChild(button);
    }

    function downloadXML(xml) {
        const blob = new Blob([xml], { type: "application/xml" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = "test-results.xml";
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }

    function isHeadless() {
        return /HeadlessChrome/.test(navigator.userAgent) ||
            /PhantomJS/.test(navigator.userAgent) ||
            (/Edg/.test(navigator.userAgent) && /Headless/.test(navigator.userAgent));
    }

    // Expose for headless environments to access results
    window.getTestResultsXML = function () {
        return generateJUnitXML(testResults);
    };
})();