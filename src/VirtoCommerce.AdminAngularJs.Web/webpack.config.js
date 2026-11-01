// webpack.config.js
const path = require('path');
const fg = require('fast-glob');
const webpack = require('webpack');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');

// The admin SPA lives in Content/wwwroot, which the module adds to the platform web root:
// the bundles land in Content/wwwroot/dist and are served at /dist, as they were from the platform.
// They are deliberately not in {module}/dist: the platform loads every module's dist/app.js as a plugin.
const webRootPath = path.resolve(__dirname, 'Content/wwwroot');
const rootPath = path.resolve(webRootPath, 'dist');

module.exports = (env, argv) => {
    const isProduction = argv.mode === 'production';

    return {
        entry: {
            vendor: ['./Content/wwwroot/src/js/vendor.js', './Content/wwwroot/css/themes/main/sass/main.sass'],
            app: [
                ...fg.sync('./Content/wwwroot/js/**/*.js'),
                ...(isProduction ? fg.sync('./Content/wwwroot/js/**/*.html') : [])
            ]
        },
        devtool: isProduction ? 'source-map' : 'eval-source-map',
        output: {
            path: rootPath,
            filename: '[name].js',
            clean: true
        },
        module: {
            rules: [{
                    test: /.css$/,
                    use: [MiniCssExtractPlugin.loader, 'css-loader'],
                },
                {
                    test: /.(jpe?g|png|gif)$/i,
                    type: 'asset/resource',
                    generator: {
                        filename: 'images/[name][ext]',
                    },
                },
                {
                    test: /\.(woff(2)?|ttf|eot|svg)(\?v=\d+\.\d+\.\d+)?$/,
                    type: 'asset/resource',
                    generator: {
                        filename: 'fonts/[name][ext]',
                    },
                },
                {
                    test: /.(sass|scss)$/,
                    use: [{
                            loader: MiniCssExtractPlugin.loader
                        },
                        {
                            loader: 'css-loader'
                        },
                        {
                            loader: 'sass-loader',
                            options: {
                                implementation: require('sass'),
                            },
                        },
                    ],
                },
                {
                    test: /\.html$/,
                    use: [
                        {
                            loader: 'ngtemplate-loader',
                            options: {
                                relativeTo: path.resolve(__dirname, './Content/wwwroot/js/'),
                                prefix: "$(Platform)/Scripts/",
                            }
                        },
                        {
                            loader: "html-loader",
                            options: {
                                sources: false,
                            }
                        }
                    ]
                },
            ],
        },
        plugins: [
            new MiniCssExtractPlugin({
                filename: 'style.css',
            }),
            new webpack.ProvidePlugin({
                $: 'jquery',
                jQuery: 'jquery',
                'window.jQuery': 'jquery',
                _: 'underscore',
            }),
        ],
        resolve: {
            extensions: ['.js', '.json'],
            alias: {
                Vendor: path.resolve(webRootPath, 'vendor')
            },
            fallback: { fs: false }
        },
    };
};
