FROM node:20-alpine AS build

WORKDIR /app

COPY modernization-web/package.json modernization-web/package-lock.json* ./modernization-web/
RUN npm ci --prefix modernization-web

COPY modernization-web ./modernization-web
RUN npm run build --prefix modernization-web

FROM nginx:1.27-alpine

COPY --from=build /app/modernization-web/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
