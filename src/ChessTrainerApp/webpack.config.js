const path = require('path');
const CopyPlugin = require('copy-webpack-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');

const outDir = path.resolve(__dirname, 'wwwroot/dist');

module.exports = [
    // 1. Styles bundle: app/site.scss -> app.bundle.css (+ static assets)
    {
        name: 'styles',
        entry: { styles: './app/site.scss' },
        output: {
            path: outDir,
            // Webpack still emits a JS file for every entry; this one is unused.
            filename: 'style.bundle.js',
            publicPath: '/'
        },
        module: {
            rules: [
                {
                    test: /\.(scss|css)$/,
                    use: [
                        { loader: MiniCssExtractPlugin.loader },
                        { loader: 'css-loader' },
                        {
                            loader: 'sass-loader',
                            options: {
                                // Sass 1.x emits noisy deprecation warnings for the
                                // Material Components 4.x SCSS we depend on (legacy
                                // `@import`, `/`-division, `red()`/`green()`/`blue()`
                                // color functions, etc.). MDC 4 is upstream-frozen
                                // legacy code, not ours, so silence warnings from
                                // any SCSS under node_modules. We also still use
                                // `@import` ourselves in app/site.scss — silence that
                                // category until we migrate to `@use`/`@forward`.
                                sassOptions: {
                                    quietDeps: true,
                                    silenceDeprecations: ['legacy-js-api', 'import']
                                }
                            }
                        }
                    ]
                }
            ]
        },
        plugins: [
            new MiniCssExtractPlugin({ filename: 'app.bundle.css' }),
            new CopyPlugin({
                patterns: [
                    { from: 'app/images', to: 'images' },
                    { from: 'app/favicon.ico', to: 'favicon.ico' }
                ]
            })
        ]
    },

    // 2. App JS bundle: app/app.js -> app.bundle.js
    {
        name: 'app',
        entry: { main: './app/app.js' },
        output: {
            path: outDir,
            filename: 'app.bundle.js',
            publicPath: '/'
        },
        module: {
            rules: [
                {
                    test: /\.js$/,
                    exclude: /node_modules/,
                    use: {
                        loader: 'babel-loader',
                        options: {
                            presets: [['@babel/preset-env']]
                        }
                    }
                },
                {
                    test: /\.(jpe?g|ico)$/,
                    type: 'asset/resource',
                    generator: { filename: '[name][ext]' }
                }
            ]
        }
    }
];