// The Aspire AppHost injects the API's address via WithReference(api).
module.exports = {
  '/api': {
    target:
      process.env['services__api__http__0'] ||
      process.env['services__api__https__0'] ||
      'http://localhost:5062',
    secure: false,
    changeOrigin: true,
  },
};
