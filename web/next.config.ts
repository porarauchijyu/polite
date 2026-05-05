import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: 'export',
  basePath: '/polite',
  images: {
    unoptimized: true,
  },
};

export default nextConfig;
