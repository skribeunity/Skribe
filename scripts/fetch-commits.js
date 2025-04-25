const fs = require('fs');
const https = require('https');

const owner = 'skribeunity';
const repo = 'Skribe';
const outFile = './DocUpdates/recent-commits.md';

const options = {
    hostname: 'api.github.com',
    path: `/repos/${owner}/${repo}/commits`,
    headers: {
        'User-Agent': 'yourusername',
        'Accept': 'application/vnd.github.v3+json',
    },
};

https.get(options, (res) => {
    let data = '';
    res.on('data', (chunk) => { data += chunk; });
    res.on('end', () => {
        const commits = JSON.parse(data);
        const content = `# Recent Commits\n\n` +
            commits.slice(0, 10).map(commit => {
                const message = commit.commit.message.split('\n')[0];
                const url = commit.html_url;
                const date = new Date(commit.commit.author.date).toLocaleDateString();
                return `- [${message}](${url}) – ${date}`;
            }).join('\n');

        fs.mkdirSync('DocUpdates', { recursive: true });
        fs.writeFileSync(outFile, content, 'utf8');
        console.log('Recent commits written to DocUpdates/recent-commits.md');
    });
}).on('error', (err) => {
    console.error('Error fetching commits:', err);
});
