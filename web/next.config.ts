import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: 'export',
  images: {
    unoptimized: true,
  },
  /* GitHub Pages のサブディレクトリで公開する場合は base     Path を追加しますが、
     今回はカスタムドメインまたはルートでの公開を想定し、必要に応じてユーザーに確認します */
};

export default nextConfig;
