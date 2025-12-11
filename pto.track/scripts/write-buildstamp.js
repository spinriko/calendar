const fs = require('fs');
const path = require('path');

let projectDir = process.argv[2] || '.';
if (typeof projectDir === 'string') {
    projectDir = projectDir.trim();
    projectDir = projectDir.replace(/^['\"]+|['\"]+$/g, '');
    projectDir = path.normalize(projectDir);
}
const stampPath = path.join(projectDir, 'wwwroot', 'dist', '.buildstamp');

try {
    fs.mkdirSync(path.dirname(stampPath), { recursive: true });
    fs.writeFileSync(stampPath, Date.now().toString());
    process.exit(0);
} catch (e) {
    console.error('Failed to write buildstamp:', e);
    process.exit(1);
}
