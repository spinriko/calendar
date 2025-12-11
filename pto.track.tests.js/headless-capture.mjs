import puppeteer from 'puppeteer';
import fs from 'fs/promises';
import path from 'path';

const url = process.argv[2] || 'http://localhost:5139/AbsencesScheduler';
const outDir = path.resolve(process.cwd(), '..', 'responses');

async function ensureOut() {
    try { await fs.mkdir(outDir, { recursive: true }); } catch (e) { }
}

(async () => {
    await ensureOut();
    const browser = await puppeteer.launch({ headless: true, args: ['--no-sandbox'] });
    const page = await browser.newPage();

    const consoleLogs = [];
    const network = [];

    page.on('console', msg => {
        const text = msg.text();
        const entry = { type: msg.type(), text, location: msg.location() };
        consoleLogs.push(entry);
    });

    page.on('response', async (response) => {
        try {
            const req = response.request();
            const url = req.url();
            const headers = response.headers();
            const status = response.status();
            let body = null;
            const ct = headers['content-type'] || '';
            // Only capture textual bodies to avoid huge binary blobs
            if (ct.startsWith('text/') || ct.includes('json') || ct.includes('javascript') || ct.includes('css')) {
                try {
                    body = await response.text();
                } catch (e) {
                    body = `<unable to read: ${e.message}>`;
                }
            } else {
                body = `<binary or omitted: ${ct}>`;
            }
            network.push({ url, status, headers, body });
        } catch (e) {
            network.push({ url: '<error>', error: e.message });
        }
    });

    page.on('requestfailed', req => {
        network.push({ url: req.url(), failed: true, failure: req.failure() });
    });

    console.log('Navigating to', url);
    try {
        await page.goto(url, { waitUntil: 'networkidle2', timeout: 30000 });
    } catch (e) {
        console.error('Navigation error', e.message);
    }

    // wait a bit for dynamic activity
    await new Promise((r) => setTimeout(r, 3000));

    // take screenshot
    const screenshotPath = path.join(outDir, 'headless-screenshot.png');
    await page.screenshot({ path: screenshotPath, fullPage: true });

    // save console and network
    await fs.writeFile(path.join(outDir, 'headless-console.json'), JSON.stringify(consoleLogs, null, 2));
    await fs.writeFile(path.join(outDir, 'headless-network.json'), JSON.stringify(network, null, 2));

    console.log('Saved console/network/screenshot to', outDir);

    await browser.close();
})();
