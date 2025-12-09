const fs = require('fs');
const path = require('path');

function usage() {
    console.error('Usage: node frontend-build-needed.js <projectDir>');
    process.exit(2);
}

if (process.argv.length < 3) usage();
const projectDir = path.resolve(process.argv[2]);
const srcDir = path.join(projectDir, 'wwwroot', 'js');
const distStamp = path.join(projectDir, 'wwwroot', 'dist', '.buildstamp');

async function getAllFiles(dir) {
    const res = [];
    async function walk(d) {
        let entries;
        try {
            entries = await fs.promises.readdir(d, { withFileTypes: true });
        } catch (e) {
            return;
        }
        for (const ent of entries) {
            const full = path.join(d, ent.name);
            if (ent.isDirectory()) {
                await walk(full);
            } else {
                res.push(full);
            }
        }
    }
    await walk(dir).catch(() => { });
    return res;
}

async function main() {
    // if dist stamp missing => need build
    if (!fs.existsSync(distStamp)) {
        console.log('true');
        return;
    }

    // get stamp mtime
    let stampStat;
    try {
        stampStat = await fs.promises.stat(distStamp);
    } catch (e) {
        console.log('true');
        return;
    }
    const stampMtime = stampStat.mtimeMs;

    // gather source files
    const srcFiles = await getAllFiles(srcDir).catch(() => []);
    if (!srcFiles || srcFiles.length === 0) {
        // no sources, nothing to build
        console.log('false');
        return;
    }

    for (const f of srcFiles) {
        if (!f.endsWith('.ts') && !f.endsWith('.js') && !f.endsWith('.css')) continue;
        try {
            const st = await fs.promises.stat(f);
            if (st.mtimeMs > stampMtime + 1) { // allow tiny clock skew
                console.log('true');
                return;
            }
        } catch (e) {
            console.log('true');
            return;
        }
    }

    console.log('false');
}

main();
