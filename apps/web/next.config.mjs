/** @type {import('next').NextConfig} */
const nextConfig = {
  // Standalone output is used for the Docker image. It relies on symlinks, which
  // require elevated permissions on Windows, so only enable it when explicitly
  // requested (the Dockerfile sets BUILD_STANDALONE=true).
  output: process.env.BUILD_STANDALONE === "true" ? "standalone" : undefined,
  reactStrictMode: true,
  eslint: {
    // Linting is run as a separate CI step; don't fail production builds on it.
    ignoreDuringBuilds: true,
  },
};

export default nextConfig;
