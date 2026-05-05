import { NextRequest, NextResponse } from 'next/server';

export async function GET(request: NextRequest) {
  const searchParams = request.nextUrl.searchParams;
  const url = searchParams.get('url');

  if (!url) {
    return NextResponse.json({ error: 'URL is required' }, { status: 400 });
  }

  try {
    const response = await fetch(url, {
      headers: {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36',
      },
    });

    const contentType = response.headers.get('content-type');
    const html = await response.text();

    // 絶対パスの書き換え（画像やスタイルシートが読み込めるように）
    const baseUrl = new URL(url);
    const origin = baseUrl.origin;
    
    let processedHtml = html
      .replace(/(src|href)="\/([^/])/g, `$1="${origin}/$2`)
      .replace(/(src|href)='\?([^/])/g, `$1='${origin}/$2`)
      // インラインスクリプトやリンクの無効化（干渉を防ぐ）
      .replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, '')
      .replace(/href="javascript:/gi, 'data-href="javascript:');

    return new NextResponse(processedHtml, {
      headers: {
        'Content-Type': 'text/html; charset=utf-8',
        'Access-Control-Allow-Origin': '*',
      },
    });
  } catch (error) {
    return NextResponse.json({ error: 'Failed to fetch the URL' }, { status: 500 });
  }
}
