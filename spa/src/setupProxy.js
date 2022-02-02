const { createProxyMiddleware } = require('http-proxy-middleware');

module.exports = function (app) {
    app.use(createProxyMiddleware("/events", {
        target: "https://localhost:8080",
        secure: false,
        ws: true
    }));
};